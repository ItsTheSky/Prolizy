using CommunityToolkit.Mvvm.ComponentModel;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private bool _isNetworkAvailable;
    
    public MainViewModel()
    {
        // Initialize network status
        IsNetworkAvailable = ConnectivityService.Instance.IsNetworkAvailable;
        
        // Subscribe to connectivity service changes
        ConnectivityService.Instance.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ConnectivityService.Instance.IsNetworkAvailable))
            {
                IsNetworkAvailable = ConnectivityService.Instance.IsNetworkAvailable;
            }
        };
    }
}