using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaTagger.Converters;

public class ZeroToBooleanConverter : IValueConverter
{
    public static ZeroToBooleanConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return System.Convert.ToInt64(value) is 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
