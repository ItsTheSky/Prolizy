using Avalonia.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.ViewModels;

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

        if (AndroidAccessManager.AndroidAccess != null)
        {
            AndroidAccessManager.AndroidAccess.BackButtonPressed += (sender, args) =>
            {
                var vm = (NewSettingsViewModel)DataContext;
                if (vm.CanGoBack)
                    vm.GoBack();
            };   
        }
    }
}