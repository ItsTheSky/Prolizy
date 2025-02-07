using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.Threading;
using Prolizy.Viewer.Android.Widgets;
using Prolizy.Viewer.Utilities.Android;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views;
using Prolizy.Viewer.Views.Panes;

namespace Prolizy.Viewer.Android;

[Activity(Label = "Prolizy",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon", 
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation
                           | ConfigChanges.ScreenSize
                           | ConfigChanges.UiMode
                           | ConfigChanges.SmallestScreenSize
                           | ConfigChanges.ScreenLayout
                           | ConfigChanges.Keyboard
                           | ConfigChanges.KeyboardHidden
                           | ConfigChanges.Navigation
)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        AndroidAccessManager.AndroidAccess = new AppWidget.AndroidAccess();

        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        
        if (intent?.Action == AppWidget.ACTION_OPEN_EDT)
        {
            OpenEDTPage();
        }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Intent?.Action == AppWidget.ACTION_OPEN_EDT)
        {
            OpenEDTPage();
        }
    }

    private void OpenEDTPage()
    {
        try
        {
            MainView.Instance.MoveToPane(typeof(TimeTablePane));
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var (item, isCurrent) = await TimeTableViewModel.GetCurrentOrNextCourse();
                if (item != null)
                {
                    await item.ItemClicked();
                }
            });
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"Error opening EDT page: {ex.Message}");
        }
    }
}