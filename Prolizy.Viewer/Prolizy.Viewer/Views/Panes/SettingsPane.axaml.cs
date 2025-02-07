using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Prolizy.Viewer.Controls.Wizard;
using Prolizy.Viewer.ViewModels;

namespace Prolizy.Viewer.Views.Panes;

public partial class SettingsPane : UserControl
{
    public SettingsPane()
    {
        InitializeComponent();
        
        DataContext = new SettingsPaneViewModel();
    }
}