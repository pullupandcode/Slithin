﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Slithin.Core;
using Slithin.ViewModels.ResourcePages;

namespace Slithin.UI.Modals;

public partial class LoginModal : UserControl
{
    public LoginModal()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        if (!Design.IsDesignMode)
        {
            DataContext = ServiceLocator.Container.Resolve<LoginModalViewModel>();
        }
    }
}
