using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Prolizy.Viewer.Utilities.Converters;

public class IntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string input) 
            return value;
        if (string.IsNullOrEmpty(input))
            return null;
        
        return int.TryParse(input, out var result) ? result : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString();
    }
}