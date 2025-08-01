using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using GameMacroAssistant.Core.Models;
using GameMacroAssistant.Core.Services;

namespace GameMacroAssistant.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMacroRecorder _macroRecorder;
    private readonly IMacroExecutor _macroExecutor;
    
    [ObservableProperty]
    private bool _isRecording;
    
    [ObservableProperty]
    private bool _isExecuting;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    [ObservableProperty]
    private Macro? _selectedMacro;
    
    public ObservableCollection<Macro> Macros { get; } = new();
    
    public MainViewModel(IMacroRecorder macroRecorder, IMacroExecutor macroExecutor)
    {
        _macroRecorder = macroRecorder;
        _macroExecutor = macroExecutor;
        
        _macroRecorder.RecordingStateChanged += OnRecordingStateChanged;
        _macroExecutor.ExecutionStateChanged += OnExecutionStateChanged;
        
        // TODO: Load saved macros from storage
        LoadMacros();
    }
    
    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private async Task StartRecordingAsync()
    {
        try
        {
            StatusMessage = "Starting recording...";
            await _macroRecorder.StartRecordingAsync();
            StatusMessage = "Recording... Press ESC to stop";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start recording: {ex.Message}";
        }
    }
    
    private bool CanStartRecording() => !IsRecording && !IsExecuting;
    
    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private async Task StopRecordingAsync()
    {
        try
        {
            await _macroRecorder.StopRecordingAsync();
            var recordedMacro = _macroRecorder.GetRecordedMacro();
            
            // TODO: Open editor with recorded macro
            StatusMessage = $"Recording completed. {recordedMacro.Steps.Count} steps recorded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to stop recording: {ex.Message}";
        }
    }
    
    private bool CanStopRecording() => IsRecording;
    
    [RelayCommand(CanExecute = nameof(CanExecuteMacro))]
    private async Task ExecuteMacroAsync()
    {
        if (SelectedMacro == null) return;
        
        try
        {
            StatusMessage = $"Executing macro: {SelectedMacro.Name}";
            var result = await _macroExecutor.ExecuteAsync(SelectedMacro);
            
            StatusMessage = result.Success 
                ? $"Macro executed successfully in {result.Duration.TotalSeconds:F1}s"
                : $"Macro execution failed: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Execution error: {ex.Message}";
        }
    }
    
    private bool CanExecuteMacro() => SelectedMacro != null && !IsRecording && !IsExecuting;
    
    [RelayCommand]
    private void EditMacro()
    {
        if (SelectedMacro == null) return;
        
        // TODO: Open editor window with selected macro
        StatusMessage = $"Opening editor for: {SelectedMacro.Name}";
    }
    
    [RelayCommand]
    private void DeleteMacro()
    {
        if (SelectedMacro == null) return;
        
        // TODO: Show confirmation dialog
        Macros.Remove(SelectedMacro);
        StatusMessage = "Macro deleted";
    }
    
    [RelayCommand]
    private void OpenSettings()
    {
        // TODO: Open settings window
        StatusMessage = "Opening settings...";
    }
    
    private void OnRecordingStateChanged(object? sender, RecordingStateChangedEventArgs e)
    {
        IsRecording = e.IsRecording;
        
        // Update command can-execute states
        StartRecordingCommand.NotifyCanExecuteChanged();
        StopRecordingCommand.NotifyCanExecuteChanged();
        ExecuteMacroCommand.NotifyCanExecuteChanged();
    }
    
    private void OnExecutionStateChanged(object? sender, MacroExecutionStateChangedEventArgs e)
    {
        IsExecuting = e.State == MacroExecutionState.Running;
        
        // Update command can-execute states
        StartRecordingCommand.NotifyCanExecuteChanged();
        ExecuteMacroCommand.NotifyCanExecuteChanged();
    }
    
    private void LoadMacros()
    {
        // TODO: Load macros from file system or database
        // Placeholder data for testing
        Macros.Add(new Macro 
        { 
            Name = "Sample Macro 1", 
            Description = "A sample macro for testing" 
        });
        Macros.Add(new Macro 
        { 
            Name = "Sample Macro 2", 
            Description = "Another sample macro" 
        });
    }
}