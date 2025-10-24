using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaTagger.Converters;

public class FilePathToImageConverter : IValueConverter
{
    public static FilePathToImageConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string filePath && File.Exists(filePath))
        {
            return Task.Run(() =>
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var image = Avalonia.Media.Imaging.Bitmap.DecodeToWidth(fs, 200);
                return (IImage)image;
            });
        }
        return Task.FromResult<IImage?>(null);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
