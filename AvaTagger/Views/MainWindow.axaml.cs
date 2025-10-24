using System.Linq;
using Avalonia.Controls;
using AvaTagger.Models.Messages;
using AvaTagger.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AvaTagger.Views;

public partial class MainWindow : Window, IRecipient<OpenFolderPickerMessage>, IRecipient<SaveFilePickerMessage>
{
    public MainWindow(MainView mainView, MainViewModel mainViewModel, IMessenger messenger)
    {
        Content = mainView;
        DataContext = mainViewModel;
        messenger.Register<OpenFolderPickerMessage>(this);
        messenger.Register<SaveFilePickerMessage>(this);
        InitializeComponent();
    }

    public async void Receive(OpenFolderPickerMessage message)
    {
        var result = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = message.Title,
        });
        if (result.FirstOrDefault() is { } folder)
        {
            message.Completion.SetResult(folder.Path.LocalPath);
        }
        else
        {
            message.Completion.SetResult(null);
        }
    }

    public async void Receive(SaveFilePickerMessage message)
    {
        var result = await StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions()
        {
            Title = message.Title,
            DefaultExtension = message.DefaultExtension,
            SuggestedFileName = message.SuggestedFileName,
        });
        if (result is { })
        {
            message.Completion.SetResult(result.Path.LocalPath);
        }
        else
        {
            message.Completion.SetResult(null);
        }
    }
}
