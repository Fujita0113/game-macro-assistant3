using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using GameMacroAssistant.Core.Services;
using GameMacroAssistant.Wpf.ViewModels;
using GameMacroAssistant.Wpf.Views;

namespace GameMacroAssistant.Wpf;

public partial class App : Application
{
    private IHost? _host;
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = CreateHostBuilder(e.Args).Build();
        await _host.StartAsync();
        
        var mainWindow = _host.Services.GetRequiredService<MainView>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }
    
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.OnExit(e);
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register Core services
                services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
                services.AddSingleton<IImageMatcher, ImageMatcher>();
                services.AddSingleton<IMacroExecutor, MacroExecutor>();
                services.AddTransient<IMacroRecorder, MacroRecorder>();
                
                // Register ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<EditorViewModel>();
                
                // Register Views
                services.AddTransient<MainView>();
                services.AddTransient<EditorView>();
                
                // Register logger
                services.AddSingleton<ILogger, ConsoleLogger>();
            });
}