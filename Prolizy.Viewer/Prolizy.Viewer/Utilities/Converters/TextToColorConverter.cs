using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Prolizy.Viewer.Utilities.Converters;

public class TextToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            if (Color.TryParse(text, out var color))
            {
                return new SolidColorBrush(color);
            }
        }
        
        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush solidColorBrush)
        {
            return solidColorBrush.Color.ToString();
        }
        
        return string.Empty;
    }
}