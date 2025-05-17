using Avalonia.Controls;
using Avalonia.Threading;
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
        
        Dispatcher.UIThread.InvokeAsync(async () => await ViewModel.RefreshBulletin());
        
        Settings.Instance.PropertyChanged += async (source, args) =>
        {
            if (args.PropertyName is nameof(Settings.Instance.BulletinUsername) or nameof(Settings.Instance.BulletinPassword))
            {
                ViewModel.UpdateClient();
                await ViewModel.RefreshBulletin();
            }
        };
        
        // Subscribe to connectivity service changes
        ConnectivityService.Instance.PropertyChanged += async (sender, args) =>
        {
            if (args.PropertyName == nameof(ConnectivityService.Instance.IsNetworkAvailable) &&
                ConnectivityService.Instance.IsNetworkAvailable &&
                ViewModel.IsNetworkUnavailable)
            {
                // When network becomes available and we previously showed the network unavailable message
                await ViewModel.RetryConnection();
            }
        };
    }

    public BulletinPaneViewModel ViewModel => (BulletinPaneViewModel)DataContext!;
}