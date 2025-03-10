﻿using System.Windows.Input;
using Slithin.API.Lib;
using Slithin.Controls.Navigation;
using Slithin.Core.MVVM;
using Slithin.Core.Services;
using Slithin.UI;
using Slithin.UI.ResourcesPage;
using Slithin.ViewModels.Modals;

namespace Slithin.ViewModels.ResourcePages;

public sealed class LoginModalViewModel : ModalBaseViewModel
{
    private readonly MarketplaceAPI _api;
    private readonly IMailboxService _mailboxService;
    private readonly ISettingsService _settingsService;
    private string _password;
    private string _username;

    public LoginModalViewModel(MarketplaceAPI api,
                               ISettingsService settingsService,
                               IMailboxService mailboxService)
    {
        OnLoad();

        ShowRegisterCommand = new DelegateCommand(_ =>
        {
            var frame = Frame.GetFrame("loginFrame");

            if (frame.CanGoBack)
            {
                frame.GoBack();
            }
            else if (frame.CanGoForward)
            {
                frame.GoForward();
            }
        });

        ConfirmCommand = new DelegateCommand(Confirm);
        _api = api;
        _settingsService = settingsService;
        _mailboxService = mailboxService;
    }

    public ICommand ConfirmCommand { get; set; }

    public string Password
    {
        get { return _password; }
        set { SetValue(ref _password, value); }
    }

    public ICommand ShowRegisterCommand { get; set; }

    public string Username
    {
        get { return _username; }
        set { SetValue(ref _username, value); }
    }

    public override void OnLoad()
    {
        base.OnLoad();

        Frame.GetFrame("loginFrame").Navigate(typeof(RegisterFramePage));
        Frame.GetFrame("loginFrame").Navigate(typeof(LoginFramePage));
    }

    private void Confirm(object obj)
    {
        _mailboxService.PostAction(() =>
        {
            _api.Authenticate(Username, Password);

            var settings = _settingsService.GetSettings();
            settings.MarketplaceCredential = new() { Username = Username, HashedPassword = Password };

            _settingsService.Save(settings);

            Frame.GetFrame("resourcesFrame").Navigate(typeof(ResourcesMainPage));
        });
    }
}
