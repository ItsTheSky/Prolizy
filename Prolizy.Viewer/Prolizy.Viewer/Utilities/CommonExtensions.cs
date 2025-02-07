using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;

namespace Prolizy.Viewer.Utilities;

public static class CommonExtensions
{
    
    public static Color ToColor(this System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
    
    public static System.Drawing.Color ToColor(this Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
    
    public static string Limit(this string input, int length)
    {
        if (input.Length <= length)
            return input;
        return string.Concat(input.AsSpan(0, length), "...");
    }
    
    public static void Add<T>(this AvaloniaList<T> children, T item, int row, int column)
        where T : Control
    {
        children.Add(item);
        
        Grid.SetRow(item, row);
        Grid.SetColumn(item, column);
    }

    public static SKColor ToSKColor(this string hex, float alpha = 1)
    {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);
        
        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        
        return new SKColor(r, g, b, (byte)(alpha * 255));
    }
    
    public static string Capitalize(this string input)
    {
        return input[..1].ToUpper() + input[1..].ToLower();
    }
    
    public static (int r, int g, int b) Brighten(int r, int g, int b, double factor)
    {
        factor = Math.Max(0, factor);
    
        int newR = Math.Min(255, (int)(r * factor));
        int newG = Math.Min(255, (int)(g * factor));
        int newB = Math.Min(255, (int)(b * factor));
    
        return (newR, newG, newB);
    }

    public static SolidColorBrush Brighten(this SolidColorBrush brush, double factor)
    {
        var color = brush.Color;
        var (r, g, b) = Brighten(color.R, color.G, color.B, factor);
        return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
    }
    
    public static Control WithRow(this Control control, int row)
    {
        Grid.SetRow(control, row);
        return control;
    }
    
    public static Control WithColumn(this Control control, int column)
    {
        Grid.SetColumn(control, column);
        return control;
    }
    
    public static SKColor LittleTransparent(this SKColor color)
    {
        return new SKColor(color.Red, color.Green, color.Blue, (byte) (color.Alpha * 0.3));
    }
    
}