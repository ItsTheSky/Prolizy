using Avalonia.Controls;
using Avalonia.Threading;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels.Sacoche;

namespace Prolizy.Viewer.Views.Panes;


public partial class SacochePane : UserControl
{
    public static SacochePane Instance { get; private set; }
    
    public SacochePane()
    {
        InitializeComponent();
        Instance = this;

        DataContext = new SacochePaneViewModel();
        
        Dispatcher.UIThread.InvokeAsync(async () => await ViewModel.RefreshState());
        Settings.Instance.PropertyChanged += async (source, args) =>
        {
            if (args.PropertyName == nameof(Settings.Instance.SacocheApiKey))
                await ViewModel.RefreshState();
        };
    }

    public SacochePaneViewModel ViewModel => (SacochePaneViewModel)DataContext!;
}