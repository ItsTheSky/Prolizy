using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Prolizy.Viewer.Android.Widgets;
using Prolizy.Viewer.Controls.Edt;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views.Panes;
using NotificationChannel = Android.App.NotificationChannel;

namespace Prolizy.Viewer.Android.Services;

/// <summary>
/// Android service implementation for widget updates.
/// </summary>
[Service(
    Exported = true, 
    ForegroundServiceType = ForegroundService.TypeDataSync,
    Permission = "android.permission.FOREGROUND_SERVICE_DATA_SYNC")]
public class AndroidWidgetUpdateService : Service, Prolizy.Viewer.Services.IWidgetUpdateService
{
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private Handler? _regularUpdateHandler;
    private Handler? _smartUpdateHandler;
    private readonly IBinder _binder;
    private bool _isRunning;
    
    // Notification ID for foreground service
    private const int ForegroundServiceNotificationId = 1001;
    
    // Notification channel ID for foreground service
    private const string ForegroundServiceChannelId = "widget_update_service";
    
    public event EventHandler<Prolizy.Viewer.Services.WidgetUpdateEventArgs>? WidgetUpdated;
    
    public AndroidWidgetUpdateService()
    {
        _binder = new WidgetUpdateBinder(this);
    }
    
    private class WidgetUpdateBinder : Binder
    {
        private readonly AndroidWidgetUpdateService _service;
        
        public WidgetUpdateBinder(AndroidWidgetUpdateService service)
        {
            _service = service;
        }
        
        public AndroidWidgetUpdateService GetService()
        {
            return _service;
        }
    }
    
    public override IBinder? OnBind(Intent? intent)
    {
        return _binder;
    }
    
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "FORCE_UPDATE")
        {
            Task.Run(async () => await ForceUpdateAsync());
        }
        else if (intent?.Action == "RESTART_SERVICE")
        {
            Task.Run(async () => await ReconfigureAsync());
        }
        else if (intent?.Action == "SMART_UPDATE")
        {
            // Extract the course end time and delay from the intent
            if (intent.HasExtra("COURSE_END_TIME") && intent.HasExtra("DELAY_MINUTES"))
            {
                var courseEndTimeTicks = intent.GetLongExtra("COURSE_END_TIME", 0);
                var delayMinutes = intent.GetIntExtra("DELAY_MINUTES", 1);
                
                if (courseEndTimeTicks > 0)
                {
                    var courseEndTime = new DateTime(courseEndTimeTicks);
                    Task.Run(async () => await ScheduleSmartUpdateAsync(courseEndTime, delayMinutes));
                }
            }
        }
        
        return StartCommandResult.Sticky;
    }
    
    public override void OnCreate()
    {
        base.OnCreate();
        Task.Run(async () => await StartAsync());
        
        // Create notification channel for foreground service
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ForegroundServiceChannelId,
                "Widget Update Service",
                NotificationImportance.Low);
            
            channel.Description = "Background service for updating the Prolizy widgets";
            channel.EnableLights(false);
            channel.EnableVibration(false);
            
            var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
        
        try
        {
            // Start as a foreground service with the appropriate type
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                // For Android 10 (API level 29) and above, use StartForeground with the type
                StartForeground(ForegroundServiceNotificationId, CreateForegroundNotification(), ForegroundService.TypeDataSync);
            }
            else
            {
                // For older Android versions
                StartForeground(ForegroundServiceNotificationId, CreateForegroundNotification());
            }
        }
        catch (Java.Lang.SecurityException ex)
        {
            Console.WriteLine($"Security exception starting foreground service: {ex.Message}");
            Console.WriteLine("Trying to start without foreground service type...");
            
            // Fall back to just starting foreground without specifying a type
            StartForeground(ForegroundServiceNotificationId, CreateForegroundNotification());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting foreground service: {ex.Message}");
        }
    }
    
    public override void OnDestroy()
    {
        Task.Run(async () => await StopAsync());
        base.OnDestroy();
    }
    
    // Create a notification for the foreground service
    private Notification CreateForegroundNotification()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        
        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        
        var builder = new Notification.Builder(this)
            .SetContentTitle("Prolizy Widget")
            .SetContentText("Keeping your widgets updated")
            .SetSmallIcon(Resource.Drawable.Icon)
            .SetContentIntent(pendingIntent);
        
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            builder.SetChannelId(ForegroundServiceChannelId);
        }
        
        return builder.Build();
    }
    
    public async Task StartAsync()
    {
        if (_isRunning)
            return;
        
        _isRunning = true;
        await ConfigureTimersAsync();
        
        // Initial update
        await ForceUpdateAsync();
        
        Console.WriteLine($"Android widget update service started with interval: {Settings.Instance.WidgetUpdateIntervalMinutes} minutes");
    }
    
    public Task StopAsync()
    {
        _isRunning = false;
        
        _regularUpdateHandler?.RemoveCallbacksAndMessages(null);
        _regularUpdateHandler = null;
        
        _smartUpdateHandler?.RemoveCallbacksAndMessages(null);
        _smartUpdateHandler = null;
        
        Console.WriteLine("Android widget update service stopped");
        
        return Task.CompletedTask;
    }
    
    public async Task ForceUpdateAsync()
    {
        if (!_isRunning)
            return;
        
        await UpdateWidgetAsync(Prolizy.Viewer.Services.WidgetUpdateType.Manual);
    }
    
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
        var delayMs = (long)(updateTime - now).TotalMilliseconds;
        
        // Configure the smart update handler
        _smartUpdateHandler?.RemoveCallbacksAndMessages(null);
        _smartUpdateHandler = new Handler(Looper.MainLooper);
        _smartUpdateHandler.PostDelayed(async () => await SmartUpdateCallback(), delayMs);
        
        Console.WriteLine($"Scheduled smart widget update for {updateTime:HH:mm:ss} " +
                         $"(in {(updateTime - now).TotalMinutes:F1} minutes)");
    }
    
    public async Task ReconfigureAsync()
    {
        if (!_isRunning)
            return;
        
        await ConfigureTimersAsync();
    }
    
    private async Task ConfigureTimersAsync()
    {
        // Remove existing callbacks
        _regularUpdateHandler?.RemoveCallbacksAndMessages(null);
        
        // Only schedule new callbacks if auto-update is enabled
        if (Settings.Instance.WidgetAutoUpdateEnabled)
        {
            var intervalMs = Settings.Instance.WidgetUpdateIntervalMinutes * 60 * 1000;
            _regularUpdateHandler = new Handler(Looper.MainLooper);
            _regularUpdateHandler.PostDelayed(async () => await RegularUpdateCallback(), intervalMs);
            
            Console.WriteLine($"Regular widget update handler configured with interval: {Settings.Instance.WidgetUpdateIntervalMinutes} minutes");
        }
        else
        {
            Console.WriteLine("Regular widget updates disabled");
        }
    }
    
    private async Task RegularUpdateCallback()
    {
        try
        {
            await UpdateWidgetAsync(Prolizy.Viewer.Services.WidgetUpdateType.Scheduled);
            
            // Schedule the next update
            if (_isRunning && Settings.Instance.WidgetAutoUpdateEnabled)
            {
                var intervalMs = Settings.Instance.WidgetUpdateIntervalMinutes * 60 * 1000;
                _regularUpdateHandler?.PostDelayed(async () => await RegularUpdateCallback(), intervalMs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RegularUpdateCallback: {ex.Message}");
        }
    }
    
    private async Task SmartUpdateCallback()
    {
        try
        {
            await UpdateWidgetAsync(Prolizy.Viewer.Services.WidgetUpdateType.Smart);
            
            // No need to reschedule, as smart updates are one-time events
            _smartUpdateHandler = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SmartUpdateCallback: {ex.Message}");
        }
    }
    
    private async Task UpdateWidgetAsync(Prolizy.Viewer.Services.WidgetUpdateType updateType)
    {
        // Ensure only one update happens at a time
        if (!await _updateLock.WaitAsync(0))
        {
            Console.WriteLine("Widget update already in progress, skipping");
            return;
        }
        
        try
        {
            Console.WriteLine($"Updating Android widget ({updateType})...");
            
            var (currentItem, isCurrent) = await TimeTableViewModel.GetCurrentOrNextCourse();
            
            if (currentItem == null)
            {
                Console.WriteLine("No course found to display in widget");
                WidgetUpdated?.Invoke(this, new Prolizy.Viewer.Services.WidgetUpdateEventArgs(false, updateType, "No course found"));
                return;
            }
            
            // Update the Android widget UI
            var context = ApplicationContext;
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(AppWidget)));
            var widgetIds = appWidgetManager.GetAppWidgetIds(componentName);
            
            if (widgetIds.Length > 0)
            {
                AppWidget.UpdateWidgetViews(context, appWidgetManager, widgetIds, currentItem, isCurrent);
                
                // Schedule the next smart update if enabled
                if (Settings.Instance.WidgetSmartUpdateEnabled && isCurrent && updateType != Prolizy.Viewer.Services.WidgetUpdateType.Smart)
                {
                    var courseEndTime = currentItem.EndTime;
                    var delayMinutes = Settings.Instance.WidgetSmartUpdateDelayMinutes;
                    
                    await ScheduleSmartUpdateAsync(courseEndTime, delayMinutes);
                }
                
                WidgetUpdated?.Invoke(this, new Prolizy.Viewer.Services.WidgetUpdateEventArgs(true, updateType));
                Console.WriteLine("Android widget updated successfully");
            }
            else
            {
                Console.WriteLine("No widgets found to update");
                WidgetUpdated?.Invoke(this, new Prolizy.Viewer.Services.WidgetUpdateEventArgs(false, updateType, "No widgets found"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Android widget: {ex.Message}");
            WidgetUpdated?.Invoke(this, new Prolizy.Viewer.Services.WidgetUpdateEventArgs(false, updateType, ex.Message));
        }
        finally
        {
            _updateLock.Release();
        }
    }
    
    /// <summary>
    /// Static method to start the service from any context.
    /// </summary>
    public static void StartService(Context context)
    {
        try
        {
            var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
            
            Console.WriteLine("Started AndroidWidgetUpdateService");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting AndroidWidgetUpdateService: {ex.Message}");
            // Try as a regular service if foreground fails
            try
            {
                context.StartService(new Intent(context, typeof(AndroidWidgetUpdateService)));
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Critical error starting service: {innerEx.Message}");
            }
        }
    }
    
    /// <summary>
    /// Static method to stop the service from any context.
    /// </summary>
    public static void StopService(Context context)
    {
        var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
        context.StopService(intent);
        
        Console.WriteLine("Stopped AndroidWidgetUpdateService");
    }
    
    /// <summary>
    /// Static method to request a force update from any context.
    /// </summary>
    public static void RequestForceUpdate(Context context)
    {
        try
        {
            var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
            intent.SetAction("FORCE_UPDATE");
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting force update: {ex.Message}");
            // Try as a regular service if foreground fails
            try
            {
                var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
                intent.SetAction("FORCE_UPDATE");
                context.StartService(intent);
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Critical error starting service: {innerEx.Message}");
            }
        }
    }
    
    /// <summary>
    /// Static method to request service reconfiguration from any context.
    /// </summary>
    public static void RequestReconfigure(Context context)
    {
        try
        {
            var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
            intent.SetAction("RESTART_SERVICE");
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting service reconfigure: {ex.Message}");
            // Try as a regular service if foreground fails
            try
            {
                var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
                intent.SetAction("RESTART_SERVICE");
                context.StartService(intent);
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Critical error starting service: {innerEx.Message}");
            }
        }
    }
    
    /// <summary>
    /// Static method to schedule a smart update from any context.
    /// </summary>
    public static void RequestSmartUpdate(Context context, DateTime courseEndTime, int delayMinutes)
    {
        try
        {
            var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
            intent.SetAction("SMART_UPDATE");
            intent.PutExtra("COURSE_END_TIME", courseEndTime.Ticks);
            intent.PutExtra("DELAY_MINUTES", delayMinutes);
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scheduling smart update: {ex.Message}");
            // Try as a regular service if foreground fails
            try
            {
                var intent = new Intent(context, typeof(AndroidWidgetUpdateService));
                intent.SetAction("SMART_UPDATE");
                intent.PutExtra("COURSE_END_TIME", courseEndTime.Ticks);
                intent.PutExtra("DELAY_MINUTES", delayMinutes);
                context.StartService(intent);
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Critical error starting service: {innerEx.Message}");
            }
        }
    }
}