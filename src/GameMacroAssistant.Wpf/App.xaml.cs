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
                
                // Register logger placeholder
                services.AddSingleton<ILogger, ConsoleLogger>();
            });
}

// Placeholder implementations for missing services
public class MacroRecorder : IMacroRecorder
{
    public event EventHandler<StepRecordedEventArgs>? StepRecorded;
    public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;
    
    public bool IsRecording { get; private set; }
    
    public Task StartRecordingAsync()
    {
        // TODO: Implement actual recording logic with Windows hooks
        IsRecording = true;
        RecordingStateChanged?.Invoke(this, new(true));
        return Task.CompletedTask;
    }
    
    public Task StopRecordingAsync()
    {
        IsRecording = false;
        RecordingStateChanged?.Invoke(this, new(false));
        return Task.CompletedTask;
    }
    
    public void SetStopKey(int virtualKeyCode)
    {
        // TODO: Set up global hotkey hook
    }
    
    public GameMacroAssistant.Core.Models.Macro GetRecordedMacro()
    {
        // TODO: Return recorded macro
        return new GameMacroAssistant.Core.Models.Macro { Name = "Recorded Macro" };
    }
    
    public void ClearRecording()
    {
        // TODO: Clear recorded steps
    }
}

public class ConsoleLogger : ILogger
{
    public void LogError(Exception? exception, string message, params object[] args)
    {
        Console.WriteLine($"ERROR: {string.Format(message, args)}");
        if (exception != null)
            Console.WriteLine(exception.ToString());
    }
    
    public void LogError(string message, params object[] args)
    {
        Console.WriteLine($"ERROR: {string.Format(message, args)}");
    }
}