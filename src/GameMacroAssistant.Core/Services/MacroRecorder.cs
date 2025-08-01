using System.Runtime.InteropServices;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public class MacroRecorder : IMacroRecorder
{
    private readonly IScreenCaptureService _screenCapture;
    private readonly ILogger _logger;
    private readonly List<Step> _recordedSteps = new();
    private bool _isRecording;
    private int _stopKey = 27; // ESC key
    private LowLevelKeyboardProc? _keyboardHookProc;
    private LowLevelMouseProc? _mouseHookProc;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;

    public bool IsRecording => _isRecording;

    public event EventHandler<StepRecordedEventArgs>? StepRecorded;
    public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;

    public MacroRecorder(IScreenCaptureService screenCapture, ILogger logger)
    {
        _screenCapture = screenCapture;
        _logger = logger;
        _keyboardHookProc = KeyboardHookProc;
        _mouseHookProc = MouseHookProc;
    }

    public async Task StartRecordingAsync()
    {
        if (_isRecording) return;

        _recordedSteps.Clear();
        _isRecording = true;

        // Set up low-level hooks for mouse and keyboard
        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc,
            GetModuleHandle(null), 0);
        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc,
            GetModuleHandle(null), 0);

        if (_keyboardHookId == IntPtr.Zero || _mouseHookId == IntPtr.Zero)
        {
            _logger.LogError("Failed to install input hooks");
            await StopRecordingAsync();
            return;
        }

        RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs(true));
        _logger.LogInformation("Macro recording started");

        await Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        if (!_isRecording) return;

        _isRecording = false;

        // Remove hooks
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs(false));
        _logger.LogInformation("Macro recording stopped. {StepCount} steps recorded", _recordedSteps.Count);

        await Task.CompletedTask;
    }

    public void SetStopKey(int virtualKeyCode)
    {
        _stopKey = virtualKeyCode;
    }

    public Macro GetRecordedMacro()
    {
        var macro = new Macro
        {
            Name = $"Recorded Macro {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Description = $"Auto-recorded macro with {_recordedSteps.Count} steps",
            Steps = new List<Step>(_recordedSteps)
        };

        // Set step order
        for (int i = 0; i < macro.Steps.Count; i++)
        {
            macro.Steps[i].Order = i;
        }

        return macro;
    }

    public void ClearRecording()
    {
        _recordedSteps.Clear();
    }

    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isRecording)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var isKeyDown = wParam == (IntPtr)WM_KEYDOWN;
            var isKeyUp = wParam == (IntPtr)WM_KEYUP;

            // Check for stop key
            if (vkCode == _stopKey && isKeyDown)
            {
                Task.Run(StopRecordingAsync);
                return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
            }

            if (isKeyDown || isKeyUp)
            {
                var step = new KeyboardStep
                {
                    Timestamp = DateTime.UtcNow,
                    VirtualKeyCode = vkCode,
                    Action = isKeyDown ? KeyAction.Press : KeyAction.Release,
                    PressTime = DateTime.UtcNow
                };

                if (isKeyUp)
                {
                    step.ReleaseTime = DateTime.UtcNow;
                }

                RecordStepAsync(step);
            }
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isRecording)
        {
            var hookStruct = Marshal.PtrToStructure<POINT>(lParam);
            var currentTime = DateTime.UtcNow;

            MouseAction? action = wParam.ToInt32() switch
            {
                WM_LBUTTONDOWN => MouseAction.Press,
                WM_LBUTTONUP => MouseAction.Release,
                WM_RBUTTONDOWN => MouseAction.Press,
                WM_RBUTTONUP => MouseAction.Release,
                WM_MBUTTONDOWN => MouseAction.Press,
                WM_MBUTTONUP => MouseAction.Release,
                WM_MOUSEMOVE => MouseAction.Move,
                _ => null
            };

            if (action.HasValue)
            {
                var button = GetMouseButton(wParam.ToInt32());
                var step = new MouseStep
                {
                    Timestamp = currentTime,
                    AbsolutePosition = new System.Drawing.Point(hookStruct.x, hookStruct.y),
                    Button = button,
                    Action = action.Value,
                    PressDownTimeMs = action == MouseAction.Press ? 100 : 0 // Default press time
                };

                RecordStepAsync(step);
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private MouseButton GetMouseButton(int wParam)
    {
        return wParam switch
        {
            WM_LBUTTONDOWN or WM_LBUTTONUP => MouseButton.Left,
            WM_RBUTTONDOWN or WM_RBUTTONUP => MouseButton.Right,
            WM_MBUTTONDOWN or WM_MBUTTONUP => MouseButton.Middle,
            _ => MouseButton.Left
        };
    }

    private async void RecordStepAsync(Step step)
    {
        try
        {
            // Capture screenshot after 50ms delay as per R-004
            await Task.Delay(50);
            var screenshot = await _screenCapture.CaptureScreenAsync();
            
            switch (step)
            {
                case MouseStep mouseStep:
                    mouseStep.ScreenshotData = screenshot;
                    break;
                case KeyboardStep keyboardStep:
                    keyboardStep.ScreenshotData = screenshot;
                    break;
            }

            _recordedSteps.Add(step);
            StepRecorded?.Invoke(this, new StepRecordedEventArgs(step));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record step");
        }
    }

    #region Windows API

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEMOVE = 0x0200;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_isRecording)
            {
                StopRecordingAsync().Wait();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}