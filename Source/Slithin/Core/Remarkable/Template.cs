﻿using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LiteDB;
using Newtonsoft.Json;
using Renci.SshNet;
using Slithin.Core.Services;

namespace Slithin.Core.Remarkable;

public class Template : INotifyPropertyChanged
{
    private IImage _image;

    public Template()
    {
        TransferCommand = new DelegateCommand(Transfer);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [JsonProperty("categories")] public string[] Categories { get; set; }

    [JsonProperty("filename")] public string Filename { get; set; }

    [JsonProperty("iconCode")] public string IconCode { get; set; }

    [JsonIgnore]
    [BsonIgnore]
    public IImage Image
    {
        get => _image;
        set
        {
            if (_image == value)
            {
                return;
            }

            _image = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Image)));
        }
    }

    [JsonProperty("landscape")] public bool Landscape { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [BsonIgnore]
    [JsonIgnore]
    public ICommand TransferCommand { get; set; }

    public void Load()
    {
        var templatesDir = ServiceLocator.Container.Resolve<IPathManager>().TemplatesDir;

        if (!Directory.Exists(templatesDir) || Image is not null)
        {
            return;
        }

        var path = Path.Combine(templatesDir, Filename);

        if (!File.Exists(path + ".png"))
        {
            return;
        }

        Image = Bitmap.DecodeToWidth(File.OpenRead(path + ".png"), 150);
    }

    private void Transfer(object obj)
    {
        var mailboxService = ServiceLocator.Container.Resolve<IMailboxService>();
        var pathManager = ServiceLocator.Container.Resolve<IPathManager>();
        var scp = ServiceLocator.Container.Resolve<ScpClient>();

        mailboxService.PostAction(() =>
        {
            //upload screen
            NotificationService.Show($"Uploading Template '{Name}'");

            scp.Upload(new FileInfo(Path.Combine(pathManager.TemplatesDir, Filename)), PathList.Templates + Filename);

            TemplateStorage.Instance.Apply();
            NotificationService.Hide();
        });
    }
}
