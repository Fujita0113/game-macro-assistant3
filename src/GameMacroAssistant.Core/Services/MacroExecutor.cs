using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface IMacroExecutor
{
    event EventHandler<StepExecutedEventArgs>? StepExecuted;
    event EventHandler<MacroExecutionStateChangedEventArgs>? ExecutionStateChanged;
    event EventHandler<ExecutionErrorEventArgs>? ExecutionError;
    
    bool IsExecuting { get; }
    
    Task<MacroExecutionResult> ExecuteAsync(Macro macro, CancellationToken cancellationToken = default);
    
    void Stop();
    
    void Pause();
    
    void Resume();
}

public class MacroExecutor : IMacroExecutor
{
    private readonly IImageMatcher _imageMatcher;
    private readonly IScreenCaptureService _screenCapture;
    private readonly ILogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isPaused;
    
    public bool IsExecuting { get; private set; }
    
    public event EventHandler<StepExecutedEventArgs>? StepExecuted;
    public event EventHandler<MacroExecutionStateChangedEventArgs>? ExecutionStateChanged;
    public event EventHandler<ExecutionErrorEventArgs>? ExecutionError;
    
    public MacroExecutor(IImageMatcher imageMatcher, IScreenCaptureService screenCapture, ILogger logger)
    {
        _imageMatcher = imageMatcher;
        _screenCapture = screenCapture;
        _logger = logger;
    }
    
    public async Task<MacroExecutionResult> ExecuteAsync(Macro macro, CancellationToken cancellationToken = default)
    {
        if (IsExecuting)
            throw new InvalidOperationException("Macro is already executing");
        
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cancellationTokenSource.Token;
        
        IsExecuting = true;
        ExecutionStateChanged?.Invoke(this, new(MacroExecutionState.Running));
        
        var result = new MacroExecutionResult
        {
            MacroId = macro.Id,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            // TODO: Apply image match thresholds from macro settings
            _imageMatcher.SetThresholds(macro.Settings.ImageMatchThreshold, macro.Settings.PixelDifferenceThreshold);
            
            foreach (var step in macro.Steps.OrderBy(s => s.Order))
            {
                token.ThrowIfCancellationRequested();
                
                while (_isPaused && !token.IsCancellationRequested)
                {
                    await Task.Delay(100, token);
                }
                
                var stepResult = await ExecuteStepAsync(step, token);
                result.StepResults.Add(stepResult);
                
                StepExecuted?.Invoke(this, new(step, stepResult));
                
                if (!stepResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = stepResult.ErrorMessage;
                    break;
                }
            }
            
            result.EndTime = DateTime.UtcNow;
            result.Success = result.StepResults.All(r => r.Success);
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Execution was cancelled";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Macro execution failed");
        }
        finally
        {
            IsExecuting = false;
            ExecutionStateChanged?.Invoke(this, new(MacroExecutionState.Stopped));
        }
        
        return result;
    }
    
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }
    
    public void Pause()
    {
        _isPaused = true;
        ExecutionStateChanged?.Invoke(this, new(MacroExecutionState.Paused));
    }
    
    public void Resume()
    {
        _isPaused = false;
        ExecutionStateChanged?.Invoke(this, new(MacroExecutionState.Running));
    }
    
    private async Task<StepExecutionResult> ExecuteStepAsync(Step step, CancellationToken cancellationToken)
    {
        var result = new StepExecutionResult
        {
            StepId = step.Id,
            StartTime = DateTime.UtcNow
        };
        
        try
        {
            switch (step)
            {
                case MouseStep mouseStep:
                    await ExecuteMouseStepAsync(mouseStep, cancellationToken);
                    break;
                    
                case KeyboardStep keyboardStep:
                    await ExecuteKeyboardStepAsync(keyboardStep, cancellationToken);
                    break;
                    
                case DelayStep delayStep:
                    await Task.Delay(delayStep.DelayMs, cancellationToken);
                    break;
                    
                case ConditionalStep conditionalStep:
                    await ExecuteConditionalStepAsync(conditionalStep, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Step type {step.Type} is not supported");
            }
            
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Step execution failed for step {StepId}", step.Id);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }
        
        return result;
    }
    
    private async Task ExecuteMouseStepAsync(MouseStep step, CancellationToken cancellationToken)
    {
        // TODO: Implement mouse action execution with Windows API
        // SetCursorPos, mouse_event, SendInput etc.
        await Task.Delay(1, cancellationToken);
    }
    
    private async Task ExecuteKeyboardStepAsync(KeyboardStep step, CancellationToken cancellationToken)
    {
        // TODO: Implement keyboard action execution with Windows API
        // SendInput with virtual key codes
        await Task.Delay(1, cancellationToken);
    }
    
    private async Task ExecuteConditionalStepAsync(ConditionalStep step, CancellationToken cancellationToken)
    {
        // TODO: Capture screen and match condition image
        // Execute appropriate step list based on match result
        await Task.Delay(1, cancellationToken);
    }
}

public class StepExecutedEventArgs : EventArgs
{
    public Step Step { get; }
    public StepExecutionResult Result { get; }
    
    public StepExecutedEventArgs(Step step, StepExecutionResult result)
    {
        Step = step;
        Result = result;
    }
}

public class MacroExecutionStateChangedEventArgs : EventArgs
{
    public MacroExecutionState State { get; }
    
    public MacroExecutionStateChangedEventArgs(MacroExecutionState state)
    {
        State = state;
    }
}

public class ExecutionErrorEventArgs : EventArgs
{
    public string ErrorCode { get; }
    public string Message { get; }
    public Exception? Exception { get; }
    
    public ExecutionErrorEventArgs(string errorCode, string message, Exception? exception = null)
    {
        ErrorCode = errorCode;
        Message = message;
        Exception = exception;
    }
}

public class MacroExecutionResult
{
    public string MacroId { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<StepExecutionResult> StepResults { get; set; } = new();
    public TimeSpan Duration => EndTime - StartTime;
}

public class StepExecutionResult
{
    public string StepId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}

public enum MacroExecutionState
{
    Stopped,
    Running,
    Paused,
    Error
}