using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using FluentAvalonia.Styling;

namespace Prolizy.Viewer.Utilities;

public static class ColorSchemeManager
{

    public static Dictionary<string, (Color, string)> Schemes = new()
    {
        { "purple", (Colors.BlueViolet, "PurpleGradient") },
        { "cyan", (Colors.DarkCyan, "CyanGradient") },
        { "green", (Colors.ForestGreen, "GreenGradient") },
        { "red", (Colors.DarkRed, "RedGradient") },
        { "base", (Colors.BlueViolet, "BasicBackgroundBrush")}
    };
    
    public static void ApplyTheme(string name) 
    {
        if (!Schemes.TryGetValue(name, out var scheme))
            return;
        
        var (color, gradient) = scheme;
        
        App.Instance.FluentAvaloniaTheme.CustomAccentColor = color;
        App.Current.Resources["CoreGradientBrush"] = App.Current.Resources[gradient];
    }

}