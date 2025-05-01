using System;
using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Provider;
using Android.Webkit;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Prolizy.Viewer.Android.Widgets;
using Prolizy.Viewer.Controls.Edt;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.Views.Panes;
using NotificationChannel = Android.App.NotificationChannel;

namespace Prolizy.Viewer.Android;

public class AndroidAccess : IAndroidAccess
{
    public static NotificationChannel[] Channels =
    [
        new("update_edt", "Mises à jours de l'EDT", NotificationImportance.Default)
        {
            Description = "Notifications de changement de l'emploi du temps",
        },
        new("update_bulletin", "Mises à jours du Bulletin", NotificationImportance.Default)
        {
            Description = "Notifications de changement de notes, absences, etc."
        }
    ];
    
    public void UpdateWidget(ScheduleItem currentItem, bool isCurrent)
    {
        try
        {
            var context = Application.Context;
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(AppWidget)));
            var ids = appWidgetManager.GetAppWidgetIds(componentName);

            DebugPane.AddDebugText($"Found {ids.Length} widgets to update");

            if (ids.Length > 0)
            {
                AppWidget.UpdateWidgetViews(context, appWidgetManager, ids, currentItem, isCurrent);
            }
            else
            {
                DebugPane.AddDebugText("No widgets found to update");
            }
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"Error in AndroidAccess.UpdateWidget: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void OpenFolder(string path)
    {
        var intent = new Intent(Intent.ActionView);
        var mimeType = MimeTypeMap.Singleton?.GetMimeTypeFromExtension(
            MimeTypeMap.GetFileExtensionFromUrl(path));

        var file = new Java.IO.File(path);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            Application.Context,
            $"{Application.Context.PackageName}.fileprovider",
            file);

        intent.SetDataAndType(uri, mimeType ?? "application/*");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
        Application.Context.StartActivity(intent);
    }

    public void RequestAddWidget()
    {
        var widgetManager = AppWidgetManager.GetInstance(Application.Context);
        var myProvider = new ComponentName(Application.Context, Java.Lang.Class.FromType(typeof(AppWidget)));

        if (widgetManager.IsRequestPinAppWidgetSupported)
        {
            widgetManager.RequestPinAppWidget(myProvider, null, null);
        }
    }

    public void AskForNotificationPermission()
    {
        var context = Application.Context;
        if (context is Activity activity)
        {
            ActivityCompat.RequestPermissions(activity, [Manifest.Permission.PostNotifications], 1);
        }
        else
        {
            var intent = new Intent(Settings.ActionAppNotificationSettings)
                .AddFlags(ActivityFlags.NewTask)
                .PutExtra(Settings.ExtraAppPackage, context.PackageName);
            context.StartActivity(intent);
        }
    }

    public bool IsNotificationPermissionGranted()
    {
        return ContextCompat.CheckSelfPermission(Application.Context, Manifest.Permission.PostNotifications) ==
               Permission.Granted;
    }

    public void InitNotifications()
    {
        var notificationManager =
            (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);
        foreach (var channel in Channels)
            notificationManager.CreateNotificationChannel(channel);
    }

    public event EventHandler? BackButtonPressed;
    
    public void OnBackButtonPressed()
    {
        BackButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    public void ShowNotification(AndroidNotification notification)
    {
        var targetChannel = notification.Channel switch
        {
            Utilities.Android.NotificationChannel.UpdateEdt => "update_edt",
            Utilities.Android.NotificationChannel.UpdateBulletin => "update_bulletin",
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var icon = notification.Channel switch
        {
            Utilities.Android.NotificationChannel.UpdateEdt => Resource.Drawable.ic_calendar,
            Utilities.Android.NotificationChannel.UpdateBulletin => Resource.Drawable.ic_bulletin,
            _ => throw new ArgumentOutOfRangeException()
        };

        var builder = new NotificationCompat.Builder(Application.Context, targetChannel)
            .SetContentTitle(notification.Title)
            .SetContentText(notification.Message)
            .SetSmallIcon(Resource.Drawable.Icon)
            .SetLargeIcon(BitmapFactory.DecodeResource(Application.Context.Resources, icon))
            .SetAutoCancel(true);

        var notificationManager = NotificationManagerCompat.From(Application.Context);
        var notificationId = new Random().Next();
        notificationManager.Notify(notificationId, builder.Build());
    }
}