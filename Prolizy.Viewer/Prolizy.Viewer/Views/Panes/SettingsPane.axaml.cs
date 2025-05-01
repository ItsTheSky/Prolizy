using Avalonia.Controls;
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