using GameMacroAssistant.Core.Models;
using System.Collections.Concurrent;

namespace GameMacroAssistant.Core.Services;

public class MacroRecorderService : IMacroRecorder
{
    private readonly IInputHookService _inputHook;
    private readonly ILogger _logger;
    private readonly List<Step> _recordedSteps = new();
    private readonly object _stepsLock = new();
    private readonly ConcurrentDictionary<MouseButton, DateTime> _mouseButtonDownTimes = new();
    private readonly ConcurrentDictionary<int, DateTime> _keyDownTimes = new();
    private int _stepOrder = 0;
    
    public event EventHandler<StepRecordedEventArgs>? StepRecorded;
    public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;
    
    public bool IsRecording { get; private set; }
    
    public MacroRecorderService(IInputHookService inputHook, ILogger logger)
    {
        _inputHook = inputHook;
        _logger = logger;
        _inputHook.InputReceived += OnInputReceived;
    }
    
    public async Task StartRecordingAsync()
    {
        if (IsRecording)
        {
            _logger.LogError("Recording is already in progress");
            return;
        }
        
        try
        {
            ClearRecording();
            _inputHook.StartHook();
            
            IsRecording = true;
            RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs(true));
            
            _logger.LogError("Macro recording started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start macro recording");
            throw;
        }
        
        await Task.CompletedTask;
    }
    
    public async Task StopRecordingAsync()
    {
        if (!IsRecording)
        {
            _logger.LogError("No recording in progress");
            return;
        }
        
        try
        {
            _inputHook.StopHook();
            IsRecording = false;
            
            RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs(false));
            
            int stepCount;
            lock (_stepsLock)
            {
                stepCount = _recordedSteps.Count;
            }
            _logger.LogError($"Macro recording stopped. Recorded {stepCount} steps");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping macro recording");
        }
        
        await Task.CompletedTask;
    }
    
    public void SetStopKey(int virtualKeyCode)
    {
        _inputHook.SetStopKey(virtualKeyCode);
    }
    
    public Macro GetRecordedMacro()
    {
        lock (_stepsLock)
        {
            var macro = new Macro
            {
                Name = $"Recorded Macro {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Description = $"Macro recorded on {DateTime.Now:yyyy-MM-dd} with {_recordedSteps.Count} steps",
                Steps = new List<Step>(_recordedSteps)
            };
            
            return macro;
        }
    }
    
    public void ClearRecording()
    {
        lock (_stepsLock)
        {
            _recordedSteps.Clear();
            _stepOrder = 0;
        }
        _mouseButtonDownTimes.Clear();
        _keyDownTimes.Clear();
    }
    
    private async void OnInputReceived(object? sender, InputEventArgs e)
    {
        if (!IsRecording) return;
        
        try
        {
            Step? step = e.InputEvent switch
            {
                MouseInputEvent mouse => await ProcessMouseEventAsync(mouse),
                KeyboardInputEvent keyboard => await ProcessKeyboardEventAsync(keyboard),
                StopKeyPressedEvent => await HandleStopKeyAsync(),
                _ => null
            };
            
            if (step != null)
            {
                lock (_stepsLock)
                {
                    step.Order = _stepOrder++;
                    _recordedSteps.Add(step);
                }
                
                StepRecorded?.Invoke(this, new StepRecordedEventArgs(step));
                
                _logger.LogError($"Recorded step: {GetStepDescription(step)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input event during recording");
        }
    }
    
    private async Task<Step?> ProcessMouseEventAsync(MouseInputEvent mouseEvent)
    {
        switch (mouseEvent.Action)
        {
            case MouseAction.Press:
                _mouseButtonDownTimes[mouseEvent.Button] = mouseEvent.Timestamp;
                return null; // Don't record press events separately
                
            case MouseAction.Release:
                if (_mouseButtonDownTimes.TryRemove(mouseEvent.Button, out var downTime))
                {
                    var pressDownTime = (int)(mouseEvent.Timestamp - downTime).TotalMilliseconds;
                    
                    return new MouseStep
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = mouseEvent.Timestamp,
                        AbsolutePosition = mouseEvent.Position,
                        Button = mouseEvent.Button,
                        Action = MouseAction.Click, // Convert press+release to click
                        PressDownTimeMs = pressDownTime,
                        ScreenshotData = mouseEvent.ScreenshotData
                    };
                }
                break;
        }
        
        return null;
    }
    
    private async Task<Step?> ProcessKeyboardEventAsync(KeyboardInputEvent keyboardEvent)
    {
        switch (keyboardEvent.Action)
        {
            case KeyAction.Press:
                _keyDownTimes[keyboardEvent.VirtualKeyCode] = keyboardEvent.Timestamp;
                
                return new KeyboardStep
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = keyboardEvent.Timestamp,
                    VirtualKeyCode = keyboardEvent.VirtualKeyCode,
                    Action = KeyAction.Press,
                    PressTime = keyboardEvent.Timestamp,
                    ScreenshotData = keyboardEvent.ScreenshotData
                };
                
            case KeyAction.Release:
                if (_keyDownTimes.TryRemove(keyboardEvent.VirtualKeyCode, out var pressTime))
                {
                    
                    return new KeyboardStep
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = keyboardEvent.Timestamp,
                        VirtualKeyCode = keyboardEvent.VirtualKeyCode,
                        Action = KeyAction.Release,
                        PressTime = pressTime,
                        ReleaseTime = keyboardEvent.Timestamp,
                        ScreenshotData = keyboardEvent.ScreenshotData
                    };
                }
                break;
        }
        
        return null;
    }
    
    private async Task<Step?> HandleStopKeyAsync()
    {
        // Stop recording when stop key is pressed
        await StopRecordingAsync();
        return null;
    }
    
    private string GetStepDescription(Step step)
    {
        return step switch
        {
            MouseStep mouse => $"Mouse {mouse.Action} {mouse.Button} at ({mouse.AbsolutePosition.X}, {mouse.AbsolutePosition.Y})",
            KeyboardStep keyboard => $"Key {keyboard.VirtualKeyCode} {keyboard.Action}",
            DelayStep delay => $"Wait {delay.DelayMs}ms",
            ConditionalStep conditional => $"Image match condition",
            _ => "Unknown step"
        };
    }
}