using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;

namespace Prolizy.Viewer.Android.Activities;

[Activity(Label = "Configure Widget")]
public class ConfigureWidgetActivity : Activity
{
    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);
        
        var appWidgetId = Intent.GetIntExtra(
            AppWidgetManager.ExtraAppwidgetId, 
            AppWidgetManager.InvalidAppwidgetId);

        // Configuration UI logic here
        
        var resultValue = new Intent();
        resultValue.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
        SetResult(Result.Ok, resultValue);
        Finish();
    }
}