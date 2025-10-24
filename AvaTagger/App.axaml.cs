using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaTagger.Services;
using AvaTagger.ViewModels;
using AvaTagger.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AvaTagger;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        builder.Services.AddSingleton<TaggerService>();
        builder.Services.AddSingleton<MainWindow>();
        builder.Services.AddSingleton<MainView>();
        builder.Services.AddSingleton<MainViewModel>();

        Host = builder.Build();

        Host.Start();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Host.Services.GetRequiredService<MainWindow>();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = Host.Services.GetRequiredService<MainView>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
