using System;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
    
    public class DebugTextWriter : TextWriter
    {
        private Action<string> _debugAction;

        public DebugTextWriter(Action<string> debugAction)
        {
            _debugAction = debugAction;
        }

        public override void WriteLine(string value)
        {
            _debugAction(value);
            base.WriteLine(value);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }


    public override void OnFrameworkInitializationCompleted()
    {
        Console.SetOut(new DebugTextWriter(DebugPane.AddDebugText));
        
        try
        {
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
            Console.WriteLine(e);
            File.WriteAllText(Paths.Build("error.txt"), e.ToString());
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