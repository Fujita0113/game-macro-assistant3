using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using GameMacroAssistant.Core.Models;
using GameMacroAssistant.Core.Services;

namespace GameMacroAssistant.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMacroRecorder _macroRecorder;
    private readonly IMacroExecutor _macroExecutor;
    private readonly IMacroStorageService _storageService;
    private string _macroDirectory;
    
    [ObservableProperty]
    private bool _isRecording;
    
    [ObservableProperty]
    private bool _isExecuting;
    
    [ObservableProperty]
    private string _statusMessage = "Ready";
    
    [ObservableProperty]
    private Macro? _selectedMacro;
    
    public ObservableCollection<Macro> Macros { get; } = new();
    
    public MainViewModel(IMacroRecorder macroRecorder, IMacroExecutor macroExecutor, IMacroStorageService storageService)
    {
        try
        {
            _macroRecorder = macroRecorder;
            _macroExecutor = macroExecutor;
            _storageService = storageService;
            
            // Set default macro directory
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _macroDirectory = Path.Combine(documentsPath, "GameMacroAssistant", "Macros");
            
            _macroRecorder.RecordingStateChanged += OnRecordingStateChanged;
            _macroExecutor.ExecutionStateChanged += OnExecutionStateChanged;
            
            StatusMessage = "Initializing...";
            _ = LoadMacrosAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialization error: {ex.Message}";
        }
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
    
    [RelayCommand]
    private async Task LoadMacroFromFileAsync()
    {
        try
        {
            // TODO: Open file dialog
            // For now, prompt for file path
            StatusMessage = "File dialog would open here...";
            
            // Example loading
            // var filePath = "path/to/macro.gma.json";
            // var macro = await _storageService.LoadMacroAsync(filePath);
            // if (macro != null) Macros.Add(macro);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load macro: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task SaveMacroToFileAsync()
    {
        if (SelectedMacro == null) return;
        
        try
        {
            // TODO: Open save file dialog
            var fileName = $"{SelectedMacro.Name}.gma.json";
            var filePath = Path.Combine(_macroDirectory, fileName);
            
            await _storageService.SaveMacroAsync(SelectedMacro, filePath);
            StatusMessage = $"Macro saved to {fileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save macro: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private async Task RefreshMacroListAsync()
    {
        await LoadMacrosAsync();
        StatusMessage = "Macro list refreshed";
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
    
    private async Task LoadMacrosAsync()
    {
        try
        {
            StatusMessage = "Loading macros...";
            
            if (!Directory.Exists(_macroDirectory))
            {
                Directory.CreateDirectory(_macroDirectory);
            }
            
            var macroFiles = await _storageService.GetMacroFilesAsync(_macroDirectory);
            
            Macros.Clear();
            
            foreach (var filePath in macroFiles)
            {
                try
                {
                    var macro = await _storageService.LoadMacroAsync(filePath);
                    if (macro != null)
                    {
                        Macros.Add(macro);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to load {Path.GetFileName(filePath)}: {ex.Message}";
                }
            }
            
            // Set Ready status after loading completes
            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load macros: {ex.Message}";
        }
    }
}