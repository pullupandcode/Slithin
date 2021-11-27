﻿using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Slithin.Core;
using Slithin.Core.Commands;
using Slithin.Core.ItemContext;
using Slithin.Core.Remarkable;
using Slithin.Core.Sync;
using Slithin.ViewModels.Pages;

namespace Slithin.ContextMenus;

[Context(UIContext.Notebook)]
public class NotebookContextMenu : IContextProvider
{
    public object ParentViewModel { get; set; }

    public bool CanHandle(object obj)
    {
        return obj is Metadata;
    }

    public ICollection<MenuItem> GetMenu(object obj)
    {
        List<MenuItem> menu = new();
        if (ParentViewModel is not NotebooksPageViewModel n)
        {
            return menu;
        }

        menu.Add(new MenuItem
        {
            Header = "Copy ID",
            Command = new DelegateCommand(async _ =>
                await Application.Current.Clipboard.SetTextAsync(((Metadata)obj).ID))
        });
#if DEBUG
        menu.Add(new MenuItem
        {
            Header = "Export",
            Items = new MenuItem[]
            {
                new()
                {
                    Header = "SVG",
                    Command = new DelegateCommand(_ =>
                        ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))
                }
                /*
                new(){Header = "PDF", Command = new DelegateCommand(_ =>
                    ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))},
                new(){Header = "PNG", Command = new DelegateCommand(_ =>
                    ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))},*/
            },
            Command = new DelegateCommand(_ =>
                ServiceLocator.Container.Resolve<ExportCommand>().Execute(obj))
        });
#endif
        menu.Add(new MenuItem
        {
            Header = "Remove", Command = new DelegateCommand(_ => n.RemoveNotebookCommand.Execute(obj))
        });
        menu.Add(new MenuItem {Header = "Rename", Command = new DelegateCommand(_ => n.RenameCommand.Execute(obj))});
        menu.Add(new MenuItem {Header = "Move to Trash", Command = new DelegateCommand(_ => MoveToTrash(obj))});

        return menu;
    }

    private void MoveToTrash(object obj)
    {
        if (obj is not Metadata md)
        {
            return;
        }

        MetadataStorage.Local.Move(md, "trash");

        var syncItem = new SyncItem
        {
            Action = SyncAction.Update, Direction = SyncDirection.ToDevice, Data = md, Type = SyncType.Notebook
        };

        ServiceLocator.SyncService.AddToSyncQueue(syncItem);
    }
}
