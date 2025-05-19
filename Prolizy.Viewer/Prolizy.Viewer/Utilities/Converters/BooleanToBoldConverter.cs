using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Prolizy.Viewer.Utilities.Converters;

public class BooleanToBoldConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return booleanValue ? FontWeight.Bold : FontWeight.Normal;
        }

        return FontWeight.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}