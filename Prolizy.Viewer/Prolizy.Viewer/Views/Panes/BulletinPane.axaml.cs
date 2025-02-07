using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.API;
using Prolizy.API.Model;
using Prolizy.Viewer.Controls.Bulletin;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Views.Panes;

public partial class BulletinPane : UserControl
{
    public static BulletinPane Instance { get; private set; }
    
    public BulletinPane()
    {
        InitializeComponent();
        Instance = this;

        DataContext = new BulletinPaneViewModel();
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await ViewModel.RefreshBulletin();
        });
        
        Settings.Instance.PropertyChanged += async (sender, args) =>
        {
            if (args.PropertyName is nameof(Settings.BulletinUsername) or nameof(Settings.BulletinPassword))
                await ViewModel.RefreshBulletin();
        };
    }
    
    public BulletinPaneViewModel ViewModel => (BulletinPaneViewModel) DataContext!;
}