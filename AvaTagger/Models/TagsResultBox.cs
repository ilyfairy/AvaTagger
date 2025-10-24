using System.Threading.Tasks;
using Avalonia.Media;
using AvaTagger.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaTagger.Models;

public partial class TagsResultBox : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Image))]
    public required partial string FilePath { get; set; }

    [ObservableProperty]
    public partial TagsResult? TagsResult { get; set; }

    [ObservableProperty]
    public partial bool IsProgress { get; set; }

    [ObservableProperty]
    public partial bool IsError { get; set; }

    public Task<IImage?> Image => (Task<IImage?>)AvaTagger.Converters.FilePathToImageConverter.Instance.Convert(FilePath, typeof(IImage), null, System.Globalization.CultureInfo.InvariantCulture)!;
}