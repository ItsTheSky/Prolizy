using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.Styling;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Prolizy.Viewer.Utilities;
using Prolizy.Viewer.ViewModels;
using Prolizy.Viewer.Views;
using Prolizy.Viewer.Views.Panes;
using SkiaSharp;

namespace Prolizy.Viewer;

public partial class App : Application
{
    public static App Instance => (App) Current!;
    public FluentAvaloniaTheme FluentAvaloniaTheme { get; private set; }
    
    public override void Initialize()
    {
        SetupExceptionHandling();
        AvaloniaXamlLoader.Load(this);
        
        foreach (var style in Styles)
        {
            if (style is not FluentAvaloniaTheme fluentAvaloniaTheme) 
                continue;
            
            FluentAvaloniaTheme = fluentAvaloniaTheme;
            break;
        }
        
        if (FluentAvaloniaTheme is null)
            throw new Exception("FluentAvaloniaTheme not found");
        
        LiveCharts.Configure(config => 
                config 
                    // you can override the theme 
                    .AddDarkTheme()

                    // In case you need a non-Latin based font, you must register a typeface for SkiaSharp
                    //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('汉')) // <- Chinese 
                    //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('あ')) // <- Japanese 
                    //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('헬')) // <- Korean 
                    //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('Ж'))  // <- Russian 

                    //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('أ'))  // <- Arabic 
                    //.UseRightToLeftSettings() // Enables right to left tooltips 

                    // finally register your own mappers
                    // you can learn more about mappers at:
                    // https://livecharts.dev/docs/avalonia/2.0.0-rc2/Overview.Mappers

                    // here we use the index as X, and the population as Y 
                    //.HasMap<City>((city, index) => new(index, city.Population)) 
            // .HasMap<Foo>( .... ) 
            // .HasMap<Bar>( .... ) 
        ); 

    }
    
    private void SetupExceptionHandling()
    {
        // UI Thread exceptions
        Dispatcher.UIThread.UnhandledException += (sender, e) =>
        {
            LogException("UI Thread", e.Exception);
        };

        // Background exceptions
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LogException("Background", e.Exception);
            e.SetObserved();
        };

        // Global exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            LogException("AppDomain", e.ExceptionObject as Exception);
        };
    }

    private void LogException(string source, Exception exception)
    {
        try
        {
            var crashReport = new StringBuilder();
            crashReport.AppendLine($"Crash Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            crashReport.AppendLine($"Source: {source}");
            crashReport.AppendLine($"Exception: {exception}");
            crashReport.AppendLine($"Stack Trace: {exception.StackTrace}");

            // Écrire dans le fichier error.txt
            File.WriteAllText(Paths.Build("error.txt"), crashReport.ToString());
            
            // Aussi écrire dans la console/debug pane
            Console.WriteLine(crashReport.ToString());
        }
        catch (Exception logException)
        {
            // Au cas où l'écriture du log échoue
            Console.WriteLine($"Failed to write crash report: {logException}");
        }
    }
    
    public class MultiTextWriter(TextWriter consoleWriter, Action<string> debugAction) : TextWriter
    {
        public TextWriter ConsoleWriter { get; } = consoleWriter;
        
        public override void WriteLine(string value)
        {
            // Écrire à la fois dans la console et appeler l'action
            consoleWriter.WriteLine(value);
            debugAction(value);
        }

        public override void Write(string value)
        {
            consoleWriter.Write(value);
            debugAction(value);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }


    public override void OnFrameworkInitializationCompleted()
    {
        Console.SetOut(new MultiTextWriter(Console.Out, DebugPane.AddDebugText));
        
        try
        {
            _ = ConnectivityService.Instance;
            
            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    desktop.MainWindow = new MainWindow { DataContext = new MainViewModel() };
                    MainView.Instance.InitializeTopLevel(desktop.MainWindow);
                    break;
                case ISingleViewApplicationLifetime singleViewPlatform:
                    singleViewPlatform.MainView = new MainView { DataContext = new MainViewModel() };
                    MainView.Instance.InitializeTopLevel(TopLevel.GetTopLevel(singleViewPlatform.MainView)!);
                    break;
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception e)
        {
            LogException("Framework Initialization", e);
            throw;
        }
    }
    
    public static SKColor GetAccentColor(bool littleTransparent = false)
    {
        var accentColor = (Color) Current!.FindResource("SystemAccentColor")!;
        var alpha = littleTransparent ? 0.3 : 1;
        return new SKColor(accentColor.R, accentColor.G, accentColor.B, (byte) (accentColor.A * alpha));
    }
}