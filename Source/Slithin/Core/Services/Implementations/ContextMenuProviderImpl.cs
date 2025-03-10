﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Slithin.Core.ItemContext;
using Slithin.Core.Remarkable;
using Slithin.Core.Remarkable.Models;

namespace Slithin.Core.Services.Implementations;

public class ContextMenuProviderImpl : IContextMenuProvider
{
    private readonly Dictionary<UIContext, List<IContextProvider>> _providers = new();

    public void AddProvider(IContextProvider provider)
    {
        var attrs = provider.GetType().GetCustomAttributes<ContextAttribute>();

        foreach (var attr in attrs)
        {
            if (_providers.ContainsKey(attr.Context))
            {
                _providers[attr.Context].Add(provider);
                continue;
            }

            var list = new List<IContextProvider>();
            list.Add(provider);

            _providers.Add(attr.Context, list);
        }
    }

    public void AddProvider(IContextProvider provider, IContextCommand command)
    {
        var attrs = command.GetType().GetCustomAttributes<ContextAttribute>();

        foreach (var attr in attrs)
        {
            if (_providers.ContainsKey(attr.Context))
            {
                _providers[attr.Context].Add(provider);
                continue;
            }

            var list = new List<IContextProvider>();
            list.Add(provider);

            _providers.Add(attr.Context, list);
        }
    }

    public ContextMenu BuildMenu<T>(UIContext context, T item, object parent = null)
    {
        if (!_providers.ContainsKey(context))
        {
            return null;
        }

        var localisationProvider = ServiceLocator.Container.Resolve<ILocalisationService>();

        var providersForContext = _providers[context];
        var availableContexts = providersForContext.Where(p => p.CanHandle(item));

        var iContextProviders = availableContexts as IContextProvider[] ?? availableContexts.ToArray();
        if (!iContextProviders.Any())
        {
            return null;
        }

        var menu = new ContextMenu
        {
            Items = iContextProviders.SelectMany(c =>
            {
                c.ParentViewModel = parent;

                if (item is Metadata md) // Do not show context menu for Up navigation folder and items in trash
                {
                    if (md.VisibleName == localisationProvider.GetString("Up ..")
                        || md.Parent == "trash")
                    {
                        return Array.Empty<MenuItem>();
                    }
                }

                return c.GetMenu(item);
            })
        };

        return menu;
    }

    public void Init()
    {
        var providerTypes = Utils.FindType<IContextProvider>();

        foreach (var providerType in providerTypes)
        {
            if (!providerType.IsAssignableFrom(typeof(CommandBasedContextMenu)))
            {
                var provider = (IContextProvider)ServiceLocator.Container.Resolve(providerType);

                AddProvider(provider);
            }
        }

        var commandTypes = Utils.FindType<IContextCommand>();
        foreach (var commandType in commandTypes)
        {
            var resolvedCommand = (IContextCommand)ServiceLocator.Container.Resolve(commandType);

            AddProvider(new CommandBasedContextMenu(resolvedCommand), resolvedCommand);
        }
    }
}
