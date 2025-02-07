namespace Prolizy.Viewer.Utilities.Converters;

// ScrollValueConverters.cs
using Avalonia.Data.Converters;
using System;
using System.Globalization;

public class IsGreaterThanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string stringParameter)
        {
            if (double.TryParse(stringParameter, out double threshold))
            {
                return doubleValue > threshold;
            }
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsLessThanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is double threshold)
        {
            return doubleValue < threshold;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}