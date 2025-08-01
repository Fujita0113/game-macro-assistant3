using GameMacroAssistant.Core.Models;
using System.Runtime.InteropServices;

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

public partial class MacroExecutor : IMacroExecutor
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
        var startTime = DateTime.UtcNow;
        
        try
        {
            // Set cursor position first
            if (!SetCursorPos(step.AbsolutePosition.X, step.AbsolutePosition.Y))
            {
                throw new InvalidOperationException($"Failed to set cursor position to {step.AbsolutePosition}");
            }
            
            // Small delay to ensure cursor position is set
            await Task.Delay(1, cancellationToken);
            
            // Execute mouse action based on type
            switch (step.Action)
            {
                case MouseAction.Press:
                    await ExecuteMousePress(step.Button, cancellationToken);
                    break;
                case MouseAction.Release:
                    await ExecuteMouseRelease(step.Button, cancellationToken);
                    break;
                case MouseAction.Click:
                    await ExecuteMouseClick(step.Button, cancellationToken);
                    break;
                case MouseAction.DoubleClick:
                    await ExecuteMouseDoubleClick(step.Button, cancellationToken);
                    break;
                case MouseAction.Move:
                    // Position already set above
                    break;
                default:
                    throw new NotSupportedException($"Mouse action {step.Action} is not supported");
            }
            
            // Validate timing accuracy as per R-014
            var duration = DateTime.UtcNow - startTime;
            if (duration.TotalMilliseconds > 15) // Max 15ms per R-014
            {
                _logger.LogError("Mouse step execution exceeded timing threshold: {Duration}ms", duration.TotalMilliseconds);
                ExecutionError?.Invoke(this, new(ErrorCodes.ERR_TIM, $"Timing accuracy exceeded: {duration.TotalMilliseconds}ms"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mouse step execution failed");
            throw;
        }
    }
    
    private async Task ExecuteKeyboardStepAsync(KeyboardStep step, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var input = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)step.VirtualKeyCode,
                        wScan = 0,
                        dwFlags = step.Action == KeyAction.Release ? KEYEVENTF_KEYUP : 0,
                        time = 0,
                        dwExtraInfo = GetMessageExtraInfo()
                    }
                }
            };
            
            var result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
            if (result != 1)
            {
                throw new InvalidOperationException($"SendInput failed for key {step.VirtualKeyCode}");
            }
            
            // Validate timing accuracy as per R-014
            var duration = DateTime.UtcNow - startTime;
            if (duration.TotalMilliseconds > 15) // Max 15ms per R-014
            {
                _logger.LogError("Keyboard step execution exceeded timing threshold: {Duration}ms", duration.TotalMilliseconds);
                ExecutionError?.Invoke(this, new(ErrorCodes.ERR_TIM, $"Timing accuracy exceeded: {duration.TotalMilliseconds}ms"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keyboard step execution failed");
            throw;
        }
        
        await Task.CompletedTask;
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

// Windows API mouse execution helper methods
partial class MacroExecutor
{
    private async Task ExecuteMousePress(MouseButton button, CancellationToken cancellationToken)
    {
        var flags = GetMouseDownFlags(button);
        mouse_event((uint)flags, 0, 0, 0, UIntPtr.Zero);
        await Task.Delay(1, cancellationToken);
    }
    
    private async Task ExecuteMouseRelease(MouseButton button, CancellationToken cancellationToken)
    {
        var flags = GetMouseUpFlags(button);
        mouse_event((uint)flags, 0, 0, 0, UIntPtr.Zero);
        await Task.Delay(1, cancellationToken);
    }
    
    private async Task ExecuteMouseClick(MouseButton button, CancellationToken cancellationToken)
    {
        var downFlags = GetMouseDownFlags(button);
        var upFlags = GetMouseUpFlags(button);
        
        mouse_event((uint)downFlags, 0, 0, 0, UIntPtr.Zero);
        await Task.Delay(1, cancellationToken);
        mouse_event((uint)upFlags, 0, 0, 0, UIntPtr.Zero);
    }
    
    private async Task ExecuteMouseDoubleClick(MouseButton button, CancellationToken cancellationToken)
    {
        await ExecuteMouseClick(button, cancellationToken);
        await Task.Delay(10, cancellationToken); // Short delay between clicks
        await ExecuteMouseClick(button, cancellationToken);
    }
    
    private static MouseEventFlags GetMouseDownFlags(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => MouseEventFlags.LEFTDOWN,
            MouseButton.Right => MouseEventFlags.RIGHTDOWN,
            MouseButton.Middle => MouseEventFlags.MIDDLEDOWN,
            _ => MouseEventFlags.LEFTDOWN
        };
    }
    
    private static MouseEventFlags GetMouseUpFlags(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => MouseEventFlags.LEFTUP,
            MouseButton.Right => MouseEventFlags.RIGHTUP,
            MouseButton.Middle => MouseEventFlags.MIDDLEUP,
            _ => MouseEventFlags.LEFTUP
        };
    }

    #region Windows API

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [Flags]
    private enum MouseEventFlags : uint
    {
        LEFTDOWN = 0x00000002,
        LEFTUP = 0x00000004,
        MIDDLEDOWN = 0x00000020,
        MIDDLEUP = 0x00000040,
        MOVE = 0x00000001,
        ABSOLUTE = 0x00008000,
        RIGHTDOWN = 0x00000008,
        RIGHTUP = 0x00000010
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern IntPtr GetMessageExtraInfo();

    #endregion
}