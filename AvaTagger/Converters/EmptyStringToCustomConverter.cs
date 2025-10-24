using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaTagger.Converters;

public class EmptyStringToCustomConverter : IValueConverter
{
    public static EmptyStringToCustomConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return parameter;
            }
            return str;
        }
        return parameter;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
