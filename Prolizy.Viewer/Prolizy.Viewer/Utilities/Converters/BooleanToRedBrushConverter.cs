using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Utilities.Converters;

public class BooleanToRedBrushConverter : IValueConverter
{
    private static readonly IBrush RedBrush = ColorMatcher.RedBrush;
    private static readonly IBrush DefaultBrush = ColorMatcher.LimeBrush;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
        {
            return RedBrush;
        }
        
        return DefaultBrush;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}