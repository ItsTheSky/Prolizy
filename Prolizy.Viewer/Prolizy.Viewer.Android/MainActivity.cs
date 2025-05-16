using System;
using System.Threading.Tasks;
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
        
        Console.WriteLine($"OnNewIntent called with action: {intent?.Action}");
        
        if (intent?.Action == AppWidget.ACTION_OPEN_EDT)
        {
            // For already running app, add a small delay to ensure UI is responsive
            Task.Run(async () => 
            {
                await Task.Delay(500);
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    Console.WriteLine("Processing widget intent from OnNewIntent");
                    OpenEDTPage();
                });
            });
        }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            base.OnCreate(savedInstanceState);
            AndroidAccessManager.AndroidAccess!.InitNotifications();
            
            Console.WriteLine($"MainActivity.OnCreate - Intent action: {Intent?.Action}");

            // Only handle widget intent after app is fully initialized
            if (Intent?.Action == AppWidget.ACTION_OPEN_EDT)
            {
                Console.WriteLine("Widget intent detected, scheduling delayed execution");
                // Use a longer delay and ensure we run on UI thread correctly
                Task.Run(async () => 
                {
                    // Wait for app to initialize more completely
                    await Task.Delay(3000);
                    
                    // Then safely dispatch to UI thread
                    await Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        Console.WriteLine("Executing delayed widget action");
                        try 
                        {
                            // Check if MainView is initialized before proceeding
                            if (MainView.Instance != null)
                            {
                                OpenEDTPage();
                                Console.WriteLine("Widget intent handled successfully");
                            }
                            else
                            {
                                Console.WriteLine("MainView.Instance is null, cannot process widget intent");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in delayed widget processing: {ex}");
                        }
                    });
                });
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
            // Make sure we're on the UI thread
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Console.WriteLine("OpenEDTPage called from non-UI thread, dispatching");
                Dispatcher.UIThread.InvokeAsync(OpenEDTPage);
                return;
            }

            Console.WriteLine("OpenEDTPage executing on UI thread");
            
            // Safety check for MainView.Instance
            if (MainView.Instance == null)
            {
                Console.WriteLine("MainView.Instance is null, cannot navigate");
                DebugPane.AddDebugText("Error: MainView.Instance is null in OpenEDTPage");
                return;
            }
            
            // Ensure the view model is not in preloading state
            if (MainView.Instance.ViewModel?.IsPreLoading == true)
            {
                Console.WriteLine("App still in preloading state, setting to false");
                MainView.Instance.ViewModel.IsPreLoading = false;
            }

            var widgetAction = Settings.Instance.WidgetOpenAction;
            Console.WriteLine($"Processing widget action: {widgetAction}");

            switch (widgetAction)
            {
                case WidgetOpenAction.OpenEdt:
                case WidgetOpenAction.OpenEdtWithDescription:
                    Console.WriteLine("Navigating to TimeTablePane");
                    MainView.Instance.MoveToPane(typeof(TimeTablePane));
                    
                    // Use Task.Run for the course fetching to avoid UI blocking
                    Task.Run(async () => 
                    {
                        try
                        {
                            var (item, _) = await TimeTableViewModel.GetCurrentOrNextCourse();
                            if (item != null && widgetAction == WidgetOpenAction.OpenEdtWithDescription)
                            {
                                await Dispatcher.UIThread.InvokeAsync(async () => 
                                {
                                    await item.ItemClicked();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting course: {ex.Message}");
                        }
                    });
                    break;
                    
                case WidgetOpenAction.OpenBulletin:
                    Console.WriteLine("Navigating to BulletinPane");
                    MainView.Instance.MoveToPane(typeof(BulletinPane));
                    break;
                    
                case WidgetOpenAction.OpenSacoche:
                    Console.WriteLine("Navigating to SacochePane");
                    MainView.Instance.MoveToPane(typeof(SacochePane));
                    break;
                    
                case WidgetOpenAction.OpenSettings:
                    Console.WriteLine("Navigating to SettingsPane");
                    MainView.Instance.MoveToPane(typeof(SettingsPane));
                    break;
                    
                default:
                    Console.WriteLine($"Unknown widget action: {widgetAction}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening EDT page: {ex}");
            DebugPane.AddDebugText($"Error opening EDT page: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public override void OnBackPressed()
    {
        if (AndroidAccessManager.AndroidAccess != null)
            ((AndroidAccess) AndroidAccessManager.AndroidAccess).OnBackButtonPressed();
    }
}