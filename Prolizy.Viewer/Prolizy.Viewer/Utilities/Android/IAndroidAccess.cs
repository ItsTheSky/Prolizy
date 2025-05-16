using System;
using Prolizy.Viewer.Controls.Edt;

namespace Prolizy.Viewer.Utilities.Android;

public interface IAndroidAccess
{
    
    public void UpdateWidget(ScheduleItem currentItem, bool isCurrent);
    
    public void OpenFolder(string path);
    
    public void RequestAddWidget();
    
    public void ShowNotification(AndroidNotification notification);
    
    public void AskForNotificationPermission();
    
    public bool IsNotificationPermissionGranted();
    
    public void InitNotifications();
    
    public void RequestWidgetReconfiguration();
    
    public event EventHandler BackButtonPressed;
}

public static class AndroidAccessManager
{
    public static IAndroidAccess? AndroidAccess { get; set; }
}

public record AndroidNotification(string Title, string Message, NotificationChannel Channel);

public enum NotificationChannel
{
    UpdateEdt,
    UpdateBulletin
}