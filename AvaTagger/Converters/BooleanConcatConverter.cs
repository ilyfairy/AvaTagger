using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AvaTagger.Converters;

public class BooleanConcatConverter : IMultiValueConverter
{
    public static BooleanConcatConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return values.Aggregate((v1, v2) => System.Convert.ToBoolean(v1) && System.Convert.ToBoolean(v2));
    }
}
