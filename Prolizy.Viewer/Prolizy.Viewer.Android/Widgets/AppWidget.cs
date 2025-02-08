using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Avalonia.Android;
using Prolizy.Viewer.Android.Activities;
using Prolizy.Viewer.Controls.Edt;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views.Panes;
using Uri = Android.Net.Uri;

namespace Prolizy.Viewer.Android.Widgets;

[BroadcastReceiver(Label = "Prolizy", Exported = true)]
[IntentFilter(new[] {
    AppWidgetManager.ActionAppwidgetUpdate
})]
[MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
public class AppWidget : AppWidgetProvider 
{
    private const int CLICK_ACTION = 1;
    public const string ACTION_OPEN_EDT = "com.prolizy.viewer.OPEN_EDT";
    
    private static void UpdateWidgetViews(Context context, AppWidgetManager appWidgetManager, int[] widgetIds, ScheduleItem item, bool isCurrent)
    {
        try 
        {
            DebugPane.AddDebugText($"Updating {widgetIds.Length} widgets with course: {item?.Subject}");
            
            var updateViews = new RemoteViews(context.PackageName, Resource.Layout.course_widget);
            
            // Configure le clic
            var intent = context.PackageManager
                .GetLaunchIntentForPackage(context.PackageName);
                
            if (intent != null)
            {
                intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ReorderToFront);
                intent.SetAction(ACTION_OPEN_EDT); // Ajout de l'action personnalisée
            }
            
            var pendingIntent = PendingIntent.GetActivity(
                context, 
                CLICK_ACTION,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );
            updateViews.SetOnClickPendingIntent(Resource.Id.Widget_Layout, pendingIntent);
            
            // Met à jour le contenu
            updateViews.SetTextViewText(Resource.Id.Widget_PanelTitle, 
                isCurrent ? "Cours Actuel" : "Prochain Cours");
            updateViews.SetTextViewText(Resource.Id.Widget_CourseTime, 
                $"{item.StartTime:HH:mm} - {item.EndTime:HH:mm}");
            updateViews.SetTextViewText(Resource.Id.Widget_CourseDate, 
                GetFriendlyDateText(item.StartTime));
            updateViews.SetTextViewText(Resource.Id.Widget_CourseTitle, 
                item.Subject ?? "Sans titre");
            updateViews.SetTextViewText(Resource.Id.Widget_CourseRoom, 
                item.Room ?? "Salle non définie");
            updateViews.SetTextViewText(Resource.Id.Widget_CourseTeacher, 
                item.Professor ?? "Professeur non défini");

            appWidgetManager.UpdateAppWidget(widgetIds, updateViews);
            DebugPane.AddDebugText("Widget views updated successfully");
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"Error updating widget views: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private static string GetFriendlyDateText(DateTime date)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var dayAfterTomorrow = today.AddDays(2);
    
        if (date.Date == today)
            return "Aujourd'hui";
        if (date.Date == tomorrow)
            return "Demain";
        if (date.Date == dayAfterTomorrow)
            return "Après-demain";
        
        // Si c'est dans la même semaine ou la semaine prochaine
        var daysUntilNextOccurrence = ((int)date.DayOfWeek - (int)DateTime.Today.DayOfWeek + 7) % 7;
        var nextOccurrence = DateTime.Today.AddDays(daysUntilNextOccurrence);
    
        if (date.Date == nextOccurrence)
            return $"{date.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"))} Prochain";
        
        // Pour tous les autres cas, afficher la date complète
        return date.ToString("dddd d MMMM", new System.Globalization.CultureInfo("fr-FR"));
    }


    private static async Task UpdateWidgetWithCurrentCourse(Context context, AppWidgetManager appWidgetManager, int[] widgetIds)
    {
        try 
        {
            var (currentItem, isCurrent) = await TimeTableViewModel.GetCurrentOrNextCourse();
            
            if (currentItem == null)
            {
                DebugPane.AddDebugText("No course found to display");
                return;
            }
            
            var handler = new Handler(Looper.MainLooper);
            handler.Post(() => UpdateWidgetViews(context, appWidgetManager, widgetIds, currentItem, isCurrent));
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"Error getting course data: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public override async void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId, Bundle? newOptions)
    {
        try
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
            if (context == null || appWidgetManager == null) return;
        
            DebugPane.AddDebugText("Widget options changed - possible initial placement");
            await ForceWidgetUpdate(context);
        }
        catch (Exception e)
        {
            DebugPane.AddDebugText($"Error during widget options change: {e.Message}");
            DebugPane.AddDebugText(e.StackTrace ?? "- No stack trace");
        }
    }

    private static async Task ForceWidgetUpdate(Context context)
    {
        try 
        {
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(AppWidget)));
            var ids = appWidgetManager.GetAppWidgetIds(componentName);
            
            if (ids.Length > 0)
            {
                await UpdateWidgetWithCurrentCourse(context, appWidgetManager, ids);
            }
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"Error during force update: {ex.Message}");
        }
    }

    public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
    {
        base.OnUpdate(context, appWidgetManager, appWidgetIds);
        DebugPane.AddDebugText($"OnUpdate called with {appWidgetIds.Length} widgets");
        
        Task.Run(() => UpdateWidgetWithCurrentCourse(context, appWidgetManager, appWidgetIds));
    }

    public override void OnEnabled(Context context)
    {
        base.OnEnabled(context);
        DebugPane.AddDebugText("Widget OnEnabled called - Initial placement");
        
        var appWidgetManager = AppWidgetManager.GetInstance(context);
        var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(AppWidget)));
        var ids = appWidgetManager.GetAppWidgetIds(componentName);
        
        if (ids.Length > 0)
        {
            Task.Run(() => UpdateWidgetWithCurrentCourse(context, appWidgetManager, ids));
        }
    }

    public class AndroidAccess : IAndroidAccess
    {
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
                    UpdateWidgetViews(context, appWidgetManager, ids, currentItem, isCurrent);
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
    }
}