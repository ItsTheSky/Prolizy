using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using Prolizy.Viewer.Controls.Edt;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views.Panes;
using Timer = System.Timers.Timer;

namespace Prolizy.Viewer.Services;

/// <summary>
/// Service that handles widget updates for desktop platforms.
/// </summary>
public class DesktopWidgetUpdateService : IWidgetUpdateService
{
    private Timer? _regularUpdateTimer;
    private Timer? _smartUpdateTimer;
    private bool _isRunning;
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private static DesktopWidgetUpdateService? _instance;
    
    public event EventHandler<WidgetUpdateEventArgs>? WidgetUpdated;
    
    private DesktopWidgetUpdateService()
    {
        // Private constructor for singleton
    }
    
    /// <summary>
    /// Gets the singleton instance of the service.
    /// </summary>
    public static DesktopWidgetUpdateService Instance => _instance ??= new DesktopWidgetUpdateService();
    
    /// <summary>
    /// Starts the widget update service.
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning)
            return;
        
        _isRunning = true;
        await ConfigureTimersAsync();
        
        // Initial update
        await ForceUpdateAsync();
        
        Console.WriteLine($"Desktop widget update service started with interval: {Settings.Instance.WidgetUpdateIntervalMinutes} minutes");
    }
    
    /// <summary>
    /// Stops the widget update service.
    /// </summary>
    public Task StopAsync()
    {
        _isRunning = false;
        
        _regularUpdateTimer?.Stop();
        _regularUpdateTimer?.Dispose();
        _regularUpdateTimer = null;
        
        _smartUpdateTimer?.Stop();
        _smartUpdateTimer?.Dispose();
        _smartUpdateTimer = null;
        
        Console.WriteLine("Desktop widget update service stopped");
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Forces an immediate update of the widget.
    /// </summary>
    public async Task ForceUpdateAsync()
    {
        if (!_isRunning)
            return;
        
        await UpdateWidgetAsync(WidgetUpdateType.Manual);
    }
    
    /// <summary>
    /// Schedules the next update based on the current course end time.
    /// </summary>
    public async Task ScheduleSmartUpdateAsync(DateTime courseEndTime, int delayMinutes)
    {
        if (!_isRunning || !Settings.Instance.WidgetSmartUpdateEnabled)
            return;
        
        // Calculate the time to wait until the course ends plus the delay
        var now = DateTime.Now;
        var updateTime = courseEndTime.AddMinutes(delayMinutes);
        
        // If the scheduled time is in the past, don't schedule
        if (updateTime <= now)
            return;
        
        // Calculate the interval in milliseconds
        var interval = (updateTime - now).TotalMilliseconds;
        
        // Configure the smart update timer
        _smartUpdateTimer?.Stop();
        _smartUpdateTimer?.Dispose();
        _smartUpdateTimer = new Timer(interval);
        _smartUpdateTimer.Elapsed += async (sender, args) => await SmartTimerElapsed(sender, args);
        _smartUpdateTimer.AutoReset = false;
        _smartUpdateTimer.Start();
        
        Console.WriteLine($"Scheduled smart widget update for {updateTime:HH:mm:ss} " +
                         $"(in {(updateTime - now).TotalMinutes:F1} minutes)");
    }
    
    /// <summary>
    /// Reconfigures the service with new settings.
    /// </summary>
    public async Task ReconfigureAsync()
    {
        if (!_isRunning)
            return;
        
        await ConfigureTimersAsync();
    }
    
    private async Task ConfigureTimersAsync()
    {
        // Configure the regular update timer based on settings
        _regularUpdateTimer?.Stop();
        _regularUpdateTimer?.Dispose();
        
        // Only create and start the timer if auto-update is enabled
        if (Settings.Instance.WidgetAutoUpdateEnabled)
        {
            var intervalMs = Settings.Instance.WidgetUpdateIntervalMinutes * 60 * 1000;
            _regularUpdateTimer = new Timer(intervalMs);
            _regularUpdateTimer.Elapsed += async (sender, args) => await RegularTimerElapsed(sender, args);
            _regularUpdateTimer.AutoReset = true;
            _regularUpdateTimer.Start();
            
            Console.WriteLine($"Regular widget update timer configured with interval: {Settings.Instance.WidgetUpdateIntervalMinutes} minutes");
        }
        else
        {
            Console.WriteLine("Regular widget updates disabled");
        }
    }
    
    private async Task RegularTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await UpdateWidgetAsync(WidgetUpdateType.Scheduled);
    }
    
    private async Task SmartTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await UpdateWidgetAsync(WidgetUpdateType.Smart);
        
        // Dispose the timer after use
        _smartUpdateTimer?.Dispose();
        _smartUpdateTimer = null;
    }
    
    private async Task UpdateWidgetAsync(WidgetUpdateType updateType)
    {
        // Ensure only one update happens at a time
        if (!await _updateLock.WaitAsync(0))
        {
            Console.WriteLine("Widget update already in progress, skipping");
            return;
        }
        
        try
        {
            Console.WriteLine($"Updating desktop widget ({updateType})...");
            
            var (currentItem, isCurrent) = await TimeTableViewModel.GetCurrentOrNextCourse();
            
            if (currentItem == null)
            {
                Console.WriteLine("No course found to display in widget");
                WidgetUpdated?.Invoke(this, new WidgetUpdateEventArgs(false, updateType, "No course found"));
                return;
            }
            
            // Update the desktop widget UI
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                // Call the appropriate widget update method for desktop
                UpdateDesktopWidget(currentItem, isCurrent);
                
                // Schedule the next smart update if enabled
                if (Settings.Instance.WidgetSmartUpdateEnabled && isCurrent && updateType != WidgetUpdateType.Smart)
                {
                    var courseEndTime = currentItem.EndTime;
                    var delayMinutes = Settings.Instance.WidgetSmartUpdateDelayMinutes;
                    
                    Task.Run(() => ScheduleSmartUpdateAsync(courseEndTime, delayMinutes));
                }
            });
            
            WidgetUpdated?.Invoke(this, new WidgetUpdateEventArgs(true, updateType));
            Console.WriteLine("Desktop widget updated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating desktop widget: {ex.Message}");
            WidgetUpdated?.Invoke(this, new WidgetUpdateEventArgs(false, updateType, ex.Message));
        }
        finally
        {
            _updateLock.Release();
        }
    }
    
    private void UpdateDesktopWidget(ScheduleItem item, bool isCurrent)
    {
        // Implementation will depend on how the desktop widget is displayed
        // For now, just a placeholder - will be implemented later
        // This could update a system tray icon, notification area widget, etc.
        
        Console.WriteLine($"Desktop widget would display: {(isCurrent ? "Current" : "Next")} course: {item.Subject} " +
                         $"at {item.StartTime:HH:mm} in {item.Room}");
    }
}