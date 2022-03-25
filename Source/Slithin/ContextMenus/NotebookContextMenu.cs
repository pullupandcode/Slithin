﻿using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Slithin.Core;
using Slithin.Core.Commands;
using Slithin.Core.Features;
using Slithin.Core.FeatureToggle;
using Slithin.Core.ItemContext;
using Slithin.Core.Remarkable;
using Slithin.Core.Services;
using Slithin.ViewModels.Pages;

namespace Slithin.ContextMenus;

[Context(UIContext.Notebook)]
public class NotebookContextMenu : IContextProvider
{
    private readonly ILocalisationService _localisationService;

    public NotebookContextMenu(ILocalisationService localisationService)
    {
        _localisationService = localisationService;
    }

    public object ParentViewModel { get; set; }

    public bool CanHandle(object obj)
    {
        return obj != null
                && obj is Metadata md
                && md.VisibleName != _localisationService.GetString("Quick sheets")
                && md.VisibleName != _localisationService.GetString("Up ..")
                && md.VisibleName != _localisationService.GetString("Trash");
    }

    public ICollection<MenuItem> GetMenu(object obj)
    {
        List<MenuItem> menu = new();
        if (ParentViewModel is not NotebooksPageViewModel n)
        {
            return menu;
        }
        if (obj is not Metadata md)
        {
            return menu;
        }

        menu.Add(new MenuItem
        {
            Header = _localisationService.GetString("Copy ID"),
            Command = new DelegateCommand(async _ =>
                await Application.Current.Clipboard.SetTextAsync(md.ID))
        });

        if (Feature<ExportFeature>.IsEnabled)
        {
            var subItems = new List<MenuItem>();

            if (Feature<ExportPdfFeature>.IsEnabled)
            {
                subItems.Add(new()
                {
                    Header = "PDF",
                    Command = new DelegateCommand(_ =>
                        ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))
                });
            }
            if (Feature<ExportPngFeature>.IsEnabled)
            {
                subItems.Add(new()
                {
                    Header = "PNG",
                    Command = new DelegateCommand(_ =>
                        ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))
                });
            }

            if (Feature<ExportSvgFeature>.IsEnabled)
            {
                subItems.Add(new()
                {
                    Header = "SVG",
                    Command = new DelegateCommand(_ =>
                        ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))
                });
            }

            menu.Add(new MenuItem
            {
                Header = _localisationService.GetString("Export"),
                Items = subItems
            });
        }

        menu.Add(new MenuItem
        {
            Header = _localisationService.GetString("Remove"),
            Command = new DelegateCommand(_ => n.RemoveNotebookCommand.Execute(obj))
        });

        if (md.Type == "CollectionType")
        {
            if (md.VisibleName != _localisationService.GetString("Trash"))
            {
                menu.Add(new MenuItem
                {
                    Header = _localisationService.GetString("Move Folder Items To Trash"),
                    Command = new DelegateCommand(_ => EmptyFolder(md))
                });
            }
        }

        menu.Add(new MenuItem { Header = _localisationService.GetString("Rename"), Command = new DelegateCommand(_ => n.RenameCommand.Execute(obj)) });

        menu.Add(new MenuItem { Header = _localisationService.GetString("Move To Trash"), Command = new DelegateCommand(_ => MoveToTrash(md)) });

        return menu;
    }

    private void EmptyFolder(Metadata md)
    {
        foreach (var childMd in MetadataStorage.Local.GetByParent(md.ID))
        {
            MetadataStorage.Local.Move(childMd, "trash");

            childMd.Upload();
        }
    }

    private void MoveToTrash(Metadata md)
    {
        MetadataStorage.Local.Move(md, "trash");

        md.Upload();

        ServiceLocator.SyncService.NotebooksFilter.Documents.Clear();

        foreach (var mds in MetadataStorage.Local.GetByParent(md.Parent))
        {
            ServiceLocator.SyncService.NotebooksFilter.Documents.Add(mds);
        }
        if (md.Parent != "")
        {
            ServiceLocator.SyncService.NotebooksFilter.Documents.Add(new Metadata { Type = "CollectionType", VisibleName = _localisationService.GetString("Up ..") });
        }

        ServiceLocator.SyncService.NotebooksFilter.SortByFolder();
    }
}
