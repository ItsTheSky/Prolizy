using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using Avalonia.Threading;
using Prolizy.Viewer.Android.Widgets;
using Prolizy.Viewer.Utilities;
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
        AndroidAccessManager.AndroidAccess = new AndroidAccess();

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
        try
        {
            base.OnCreate(savedInstanceState);
            AndroidAccessManager.AndroidAccess!.InitNotifications();

            if (Intent?.Action == AppWidget.ACTION_OPEN_EDT)
            {
                OpenEDTPage();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Crash lors de l'initialisation : {ex}");
            throw;
        }
    }

    private void OpenEDTPage()
    {
        try
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                switch (Settings.Instance.WidgetOpenAction)
                {
                    case WidgetOpenAction.OpenEdt:
                    case WidgetOpenAction.OpenEdtWithDescription:
                        MainView.Instance.MoveToPane(typeof(TimeTablePane));
                        var (item, isCurrent) = await TimeTableViewModel.GetCurrentOrNextCourse();
                        if (item != null && Settings.Instance.WidgetOpenAction == WidgetOpenAction.OpenEdtWithDescription)
                            await item.ItemClicked();
                        break;
                        
                    case WidgetOpenAction.OpenBulletin:
                        MainView.Instance.MoveToPane(typeof(BulletinPane));
                        break;
                    case WidgetOpenAction.OpenSacoche:
                        MainView.Instance.MoveToPane(typeof(SacochePane));
                        break;
                    case WidgetOpenAction.OpenSettings:
                        MainView.Instance.MoveToPane(typeof(SettingsPane));
                        break;
                }
            });
        }
        catch (Exception ex)
        {
            DebugPane.AddDebugText($"Error opening EDT page: {ex.Message}");
        }
    }

    public override void OnBackPressed()
    {
        if (AndroidAccessManager.AndroidAccess != null)
            ((AndroidAccess) AndroidAccessManager.AndroidAccess).OnBackButtonPressed();
    }
}