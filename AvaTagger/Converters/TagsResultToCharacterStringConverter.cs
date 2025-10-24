using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using AvaTagger.Services;

namespace AvaTagger.Converters;

public class TagsResultToCharacterStringConverter : IValueConverter
{
    public static TagsResultToCharacterStringConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TagsResult tagsResult && tagsResult.CharacterTags.Count > 0)
        {
            return string.Join(", ", tagsResult.CharacterTags.Select(item => $"{item.Name}"));
        }
        return "<null>";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
