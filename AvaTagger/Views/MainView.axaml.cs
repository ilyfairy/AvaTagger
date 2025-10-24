using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AvaTagger.ViewModels;

namespace AvaTagger.Views;

public partial class MainView : UserControl
{
    public MainViewModel ViewModel { get; }

    public MainView(MainViewModel mainViewModel)
    {
        ViewModel = mainViewModel;
        DataContext = mainViewModel;
        Loaded += MainView_Loaded;
        InitializeComponent();
    }

    private async void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.Initialize();
    }

    private async void UserControl_Drop(object? sender, Avalonia.Input.DragEventArgs e)
    {
        if (ViewModel.Loading)
        {
            return;
        }

        if (e.DataTransfer.TryGetFiles() is [..] files)
        {
            await ViewModel.LoadImages(files.Select(v => v.Path.LocalPath));
        }

        //var formats = e.DataTransfer.Formats;
        //foreach (var item in formats)
        //{
        //    var a = e.DataTransfer.GetItems(item);
        //}
    }
}
