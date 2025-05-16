using System;
using System.Threading.Tasks;

namespace Prolizy.Viewer.Services;

/// <summary>
/// Interface for the service that handles widget updates, independent of the main application lifecycle.
/// </summary>
public interface IWidgetUpdateService
{
    /// <summary>
    /// Starts the widget update service.
    /// </summary>
    Task StartAsync();
    
    /// <summary>
    /// Stops the widget update service.
    /// </summary>
    Task StopAsync();
    
    /// <summary>
    /// Forces an immediate update of the widget.
    /// </summary>
    Task ForceUpdateAsync();
    
    /// <summary>
    /// Schedules the next update based on the current course end time.
    /// </summary>
    /// <param name="courseEndTime">The end time of the current course.</param>
    /// <param name="delayMinutes">Additional delay in minutes after the course ends.</param>
    Task ScheduleSmartUpdateAsync(DateTime courseEndTime, int delayMinutes);
    
    /// <summary>
    /// Reconfigures the service with new settings.
    /// </summary>
    Task ReconfigureAsync();
    
    /// <summary>
    /// Event that fires when the widget is updated.
    /// </summary>
    event EventHandler<WidgetUpdateEventArgs> WidgetUpdated;
}

/// <summary>
/// Event arguments for widget update events.
/// </summary>
public class WidgetUpdateEventArgs : EventArgs
{
    /// <summary>
    /// Indicates whether the update was successful.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// The error message if the update failed.
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// The time the update was performed.
    /// </summary>
    public DateTime UpdateTime { get; }
    
    /// <summary>
    /// The type of update that was performed.
    /// </summary>
    public WidgetUpdateType UpdateType { get; }
    
    public WidgetUpdateEventArgs(bool success, WidgetUpdateType updateType, string? errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
        UpdateTime = DateTime.Now;
        UpdateType = updateType;
    }
}

/// <summary>
/// Enum representing the type of widget update.
/// </summary>
public enum WidgetUpdateType
{
    /// <summary>
    /// Regular scheduled update.
    /// </summary>
    Scheduled,
    
    /// <summary>
    /// Manual update requested by the user.
    /// </summary>
    Manual,
    
    /// <summary>
    /// Smart update based on course end time.
    /// </summary>
    Smart
}