using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views.SettingsMenu.Sub;

namespace Prolizy.Viewer.Views.SettingsMenu;

public partial class NewSettingsPane : UserControl
{
    public NewSettingsPane()
    {
        InitializeComponent();

        SettingsFrame.NavigateToType(typeof(SettingsSub), null, new FrameNavigationOptions()
        {
            IsNavigationStackEnabled = false,
            TransitionInfoOverride = new SuppressNavigationTransitionInfo()
        });
        SettingsFrame.NavigateToType(typeof(SettingsMenu), null, new FrameNavigationOptions
        {
            IsNavigationStackEnabled = true,
            TransitionInfoOverride = new SuppressNavigationTransitionInfo()
        });
        DataContext = new NewSettingsViewModel(this, SettingsFrame);
    }
}