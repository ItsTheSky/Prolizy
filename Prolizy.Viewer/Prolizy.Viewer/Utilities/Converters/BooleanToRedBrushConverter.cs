using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Prolizy.Viewer.Utilities;

namespace Prolizy.Viewer.Utilities.Converters;

public class BooleanToRedBrushConverter : IValueConverter
{
    private static readonly IBrush RedBrush = new SolidColorBrush(Color.Parse(ColorMatcher.TailwindColors["red"]));
    private static readonly IBrush DefaultBrush = null;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRed && isRed)
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