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
        { "cyan", (Color.Parse("#065F5F"), "CyanGradient") },
        { "green", (Color.Parse("#145F06"), "GreenGradient") },
        { "red", (Color.Parse("#5F1506"), "RedGradient") },
        
        { "midnight", (Color.Parse("#404A57"), "MidnightSlateGradient") },
        { "deep_ocean", (Color.Parse("#466885"), "DeepOceanGradient") },
        
        { "emerald", (Color.Parse("#2A9773"), "EmeraldGradient")},
        { "sky", (Color.Parse("#44A0D8"), "SkyGradient")},
        { "rose", (Color.Parse("#C2498C"), "RoseGradient")},
        
        { "cappuccino", (Color.Parse("#C28B28"), "CappuccinoGradient")},
        { "tiramisu", (Color.Parse("#BD7865"), "TiramisuGradient")},
        
        { "base", (Colors.BlueViolet, "BasicBackgroundBrush")},
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