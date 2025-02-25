using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Prolizy.Viewer.Utilities;

public partial class ConnectivityService : ObservableObject
{
    private static ConnectivityService _instance;
    private static readonly object _lock = new();
    
    private readonly Timer _connectivityCheckTimer;
    private readonly HttpClient _httpClient;
    
    [ObservableProperty] private bool _isNetworkAvailable;
    [ObservableProperty] private DateTime _lastSuccessfulConnection;
    
    // Singleton instance
    public static ConnectivityService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ConnectivityService();
                }
            }
            return _instance;
        }
    }
    
    private ConnectivityService()
    {
        _httpClient = new HttpClient();
        
        // Initialize with the assumption that network is available
        IsNetworkAvailable = true;
        LastSuccessfulConnection = DateTime.Now;
        
        // Set up a timer to check connectivity periodically
        _connectivityCheckTimer = new Timer(30000); // Check every 30 seconds
        _connectivityCheckTimer.Elapsed += async (sender, args) => await CheckConnectivity();
        _connectivityCheckTimer.Start();
        
        // Run an initial connectivity check
        Dispatcher.UIThread.InvokeAsync(async () => await CheckConnectivity());
    }
    
    public async Task<bool> CheckConnectivity()
    {
        try
        {
            // Try to reach a reliable endpoint (use Google's DNS which is generally reliable)
            var response = await _httpClient.GetAsync("https://dns.google.com/", HttpCompletionOption.ResponseHeadersRead);
            IsNetworkAvailable = response.IsSuccessStatusCode;
            
            if (IsNetworkAvailable)
            {
                LastSuccessfulConnection = DateTime.Now;
            }
            
            return IsNetworkAvailable;
        }
        catch (Exception)
        {
            // If any exception occurs, assume network is unavailable
            IsNetworkAvailable = false;
            return false;
        }
    }
    
    // Method that can be called when an operation fails to see if it's likely 
    // due to connectivity issues
    public async Task<bool> IsNetworkIssue()
    {
        // If we already know there's no connectivity, we can avoid the check
        if (!IsNetworkAvailable)
            return true;
        
        // Otherwise, do a fresh check
        return !(await CheckConnectivity());
    }
}