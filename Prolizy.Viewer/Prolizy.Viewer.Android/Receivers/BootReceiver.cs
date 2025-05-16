using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Prolizy.Viewer.Android.Services;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Android.Receivers;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;
        
        // Only process BOOT_COMPLETED intent
        if (intent.Action != Intent.ActionBootCompleted) return;

        try
        {
            DebugPane.AddDebugText("BootReceiver: Device boot detected");
            
            // Check if widget updates are enabled
            if (!Settings.Instance.WidgetAutoUpdateEnabled)
            {
                DebugPane.AddDebugText("BootReceiver: Widget auto-updates are disabled, not starting service");
                return;
            }
            
            // Start the widget update service
            DebugPane.AddDebugText("BootReceiver: Starting widget update service");
            AndroidWidgetUpdateService.StartService(context);
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"BootReceiver error: {ex.Message}");
        }
    }
}