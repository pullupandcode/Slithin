﻿using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Renci.SshNet;
using Slithin.Core;
using Slithin.Core.Remarkable;
using Slithin.Core.Services;
using Slithin.UI.ContextualMenus;
using Slithin.ViewModels.Pages;
using Slithin.Core.Menu;
using Slithin.Core.Remarkable.Models;

namespace Slithin.UI.Pages;

[PreserveIndex(1)]
[PageIcon("Codeicons.Notebook")]
public class NotebooksPage : UserControl, IPage
{
    public NotebooksPage()
    {
        InitializeComponent();

        DataContext = ServiceLocator.Container.Resolve<NotebooksPageViewModel>();
    }

    public string Title => "Notebooks";

    public Control GetContextualMenu()
    {
        return new NotebooksContextualMenu();
    }

    bool IPage.IsEnabled()
    {
        return true;
    }

    public bool UseContextualMenu()
    {
        return true;
    }

    private void DragOver(object sender, DragEventArgs e)
    {
        // Only allow Copy or Link as Drop Operations.
        e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);

        // Only allow if the dragged data contains text or filenames.
        if (!e.Data.Contains(DataFormats.Text)
            && !e.Data.Contains(DataFormats.FileNames))
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Drop(object sender, DragEventArgs e)
    {
        var pathManager = ServiceLocator.Container.Resolve<IPathManager>();
        var localisation = ServiceLocator.Container.Resolve<ILocalisationService>();

        var notebooksDir = pathManager.NotebooksDir;

        if (e.Data.Contains(DataFormats.FileNames))
        {
            foreach (var filename in e.Data.GetFileNames())
            {
                var id = Guid.NewGuid().ToString().ToLower();

                var importProviderFactory = ServiceLocator.Container.Resolve<IImportProviderFactory>();
                var provider = importProviderFactory.GetImportProvider(".pdf", filename);

                var cnt = new ContentFile { FileType = provider == null ? "epub" : "pdf" };

                if (cnt.FileType == "pdf" || cnt.FileType == "epub")
                {
                    var md = new Metadata
                    {
                        ID = id,
                        Parent = ServiceLocator.SyncService.NotebooksFilter.Folder,
                        Content = cnt,
                        Version = 1,
                        Type = "DocumentType",
                        VisibleName = Path.GetFileNameWithoutExtension(filename)
                    };
                    MetadataStorage.Local.AddMetadata(md, out _);
                    ServiceLocator.SyncService.NotebooksFilter.Documents.Add(md);

                    provider = importProviderFactory.GetImportProvider($".{cnt.FileType}", filename);

                    if (provider != null)
                    {
                        var inputStrm = provider.Import(File.OpenRead(filename));
                        var outputStrm =
                            File.OpenWrite(Path.Combine(notebooksDir, md.ID + "." + Path.GetExtension(filename)));
                        inputStrm.CopyTo(outputStrm);

                        outputStrm.Close();
                        inputStrm.Close();

                        md.Save();

                        md.Upload();

                        var scp = ServiceLocator.Container.Resolve<ScpClient>();

                        scp.Uploading += (s, e) =>
                        {
                            NotificationService.ShowProgress(
                                localisation.GetStringFormat(
                                    "Uploading '{0}': {1}", md.VisibleName, e.Filename)
                                , (int)e.Uploaded, (int)e.Size);
                        };

                        scp.Upload(new FileInfo(Path.Combine(notebooksDir, md.ID + "." + Path.GetExtension(filename))),
                            PathList.Documents + md.ID + "." + Path.GetExtension(filename));
                    }
                    else
                    {
                        DialogService.OpenError(localisation.GetStringFormat("The filetype '{0}' is not supported", Path.GetExtension(filename)));
                    }
                }
                else
                {
                    DialogService.OpenError(localisation.GetStringFormat("The filetype '{0}' is not supported", Path.GetExtension(filename)));
                }
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }
}
