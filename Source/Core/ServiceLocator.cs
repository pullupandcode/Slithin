﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Actress;
using Avalonia;
using Avalonia.Threading;
using LiteDB;
using Newtonsoft.Json;
using Renci.SshNet;
using Slithin.Controls;
using Slithin.Core.Remarkable;
using Slithin.Core.Sync;
using Slithin.Core.Sync.Repositorys;
using Slithin.Messages;
using Slithin.ViewModels;

namespace Slithin.Core
{
    public static class ServiceLocator
    {
        public static SshClient Client;
        public static string ConfigBaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Slithin");

        public static LiteDatabase Database;

        public static DeviceRepository Device;

        public static LocalRepository Local;

        public static MailboxProcessor<AsynchronousMessage> Mailbox;

        public static string NotebooksDir;

        public static ScpClient Scp;

        public static SynchronisationService SyncService;

        public static string TemplatesDir;

        public static ConnectionWindowViewModel GetLoginCredentials()
        {
            var collection = Database.GetCollection<ConnectionWindowViewModel>();

            if (collection.Count() == 1)
            {
                return collection.FindAll().First();
            }
            else
            {
                return new();
            }
        }

        public static void Init()
        {
            NotebooksDir = Path.Combine(ConfigBaseDir, "Notebooks");
            TemplatesDir = Path.Combine(ConfigBaseDir, "Templates");

            if (!Directory.Exists(ConfigBaseDir))
            {
                Directory.CreateDirectory(ConfigBaseDir);
                Directory.CreateDirectory(TemplatesDir);
                Directory.CreateDirectory(NotebooksDir);

                File.WriteAllText(Path.Combine(ConfigBaseDir, "templates.json"), "{\"templates\": []}");
            }

            Database = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Slithin", "slithin.db"));

            Device = new();
            Local = new();

            SyncService = new();

            Mailbox = MailboxProcessor.Start<AsynchronousMessage>(
                async (_) =>
                {
                    while (true)
                    {
                        var msg = await _.Receive();

                        MessageRouter.Route(msg);
                    }
                }
                );

            ServiceLocator.InitMessageRouter();
        }

        public static void InitMessageRouter()
        {
            MessageRouter.Register<SyncMessage>(_ =>
            {
                if (_.Item.Direction == SyncDirection.ToDevice)
                {
                    switch (_.Item.Type)
                    {
                        case SyncType.Template:
                            if (_.Item.Action == SyncAction.Add)
                            {
                                Device.Add((Template)_.Item.Data);
                            }
                            else if (_.Item.Action == SyncAction.Remove)
                            {
                                Device.Remove((Template)_.Item.Data);
                            }
                            break;

                        case SyncType.TemplateConfig:
                            Scp.Upload(new FileInfo(Path.Combine(TemplatesDir, "templates.json")), PathList.Templates + "/templates.json");
                            break;

                        case SyncType.Notebook:
                            if (_.Item.Action == SyncAction.Remove)
                            {
                                Device.Remove((Metadata)_.Item.Data);
                            }
                            break;
                    }
                }
                else
                {
                }
            });

            MessageRouter.Register<InitStorageMessage>(async _ =>
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Device.GetTemplates();
                });

                Mailbox.Post(new DownloadNotebooksMessage());
            });

            MessageRouter.Register<AttentionRequiredMessage>(async _ =>
            {
                var result = await DialogService.ShowDialog(_.Text);

                if (result)
                {
                    _.Action(_);
                }
            });

            MessageRouter.Register<DownloadNotebooksMessage>(_ =>
            {
                NotificationService.Show("Downloading Notebook Metadata");

                var cmd = Client.RunCommand("ls -p " + PathList.Documents);
                var allFilenames = cmd.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(_ => !_.EndsWith(".zip") && !_.EndsWith(".zip.part"));
                var mds = new List<Metadata>();
                var mdFilenames = allFilenames.Where(_ => _.EndsWith(".metadata"));

                foreach (var md in mdFilenames)
                {
                    var mdContent = Client.RunCommand("cat " + PathList.Documents + "/" + md).Result;
                    var mdObj = JsonConvert.DeserializeObject<Metadata>(mdContent);

                    mdObj.ID = Path.GetFileNameWithoutExtension(md);

                    if (File.Exists(Path.Combine(NotebooksDir, md)))
                    {
                        var mdLocalObj = JsonConvert.DeserializeObject<Metadata>(File.ReadAllText(Path.Combine(NotebooksDir, md)));

                        if (!mdObj.Deleted)
                        {
                            if (mdObj.Version > mdLocalObj.Version)
                            {
                                if (mdObj.Type == MetadataType.DocumentType)
                                {
                                    mds.Add(mdObj);
                                }

                                File.WriteAllText(Path.Combine(NotebooksDir, md), mdContent);
                            }
                        }
                    }
                    else
                    {
                        if (mdObj.Type == MetadataType.DocumentType)
                        {
                            mds.Add(mdObj);
                        }

                        File.WriteAllText(Path.Combine(NotebooksDir, md), mdContent);
                    }

                    if (mdObj.Type == MetadataType.CollectionType && mdObj.Parent == "")
                    {
                        MetadataStorage.Add(mdObj);
                        SyncService.NotebooksFilter.Documents.Add(mdObj);
                    }
                }

                var notebook = 0;
                foreach (var md in mds)
                {
                    if (allFilenames.Contains(md.ID + "/"))
                    {
                        NotificationService.Show($"Downloading File {notebook}/{allFilenames.Where(_ => !_.EndsWith(".metadata")).Count()}");

                        var directoryInfo = new DirectoryInfo(Path.Combine(NotebooksDir, md.ID));
                        if (!directoryInfo.Exists)
                        {
                            directoryInfo.Create();
                        }

                        Scp.Download(PathList.Documents + "/" + md.ID, directoryInfo);
                        notebook++;

                        MetadataStorage.Add(md);

                        if (md.Parent == "")
                        {
                            SyncService.NotebooksFilter.Documents.Add(md);
                        }
                    }
                }

                var otherfiles = allFilenames.Where(_ => !_.EndsWith(".metadata") && !_.EndsWith("/"));

                foreach (var filename in otherfiles)
                {
                    NotificationService.Show($"Downloading File {notebook}/{allFilenames.Where(_ => !_.EndsWith(".metadata")).Count()}");
                    Scp.Download(PathList.Documents + "/" + filename, new FileInfo(Path.Combine(NotebooksDir, filename)));
                    notebook++;
                }

                NotificationService.Hide();
            });

            MessageRouter.Register<HideStatusMessage>(_ =>
            {
                NotificationService.Hide();
            });

            MessageRouter.Register<ShowStatusMessage>(_ =>
            {
                NotificationService.Show(_.Message);
            });
        }

        public static void RememberLoginCredencials(ConnectionWindowViewModel viewModel)
        {
            var collection = Database.GetCollection<ConnectionWindowViewModel>();

            if (collection.Count() == 1)
            {
                //collection.Update(viewModel);
            }
            else
            {
                collection.Insert(viewModel);
            }
        }
    }
}
