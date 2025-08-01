using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface IInputHookService
{
    event EventHandler<InputEventArgs>? InputReceived;
    bool IsHooked { get; }
    void StartHook();
    void StopHook();
    void SetStopKey(int virtualKeyCode);
}

public class WindowsInputHook : IInputHookService
{
    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    
    private readonly ILogger _logger;
    private readonly IScreenCaptureService _screenCapture;
    
    private LowLevelMouseProc _mouseProc;
    private LowLevelKeyboardProc _keyboardProc;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private IntPtr _keyboardHookId = IntPtr.Zero;
    private int _stopKey = 27; // ESC key by default
    
    public event EventHandler<InputEventArgs>? InputReceived;
    public bool IsHooked => _mouseHookId != IntPtr.Zero && _keyboardHookId != IntPtr.Zero;
    
    public WindowsInputHook(ILogger logger, IScreenCaptureService screenCapture)
    {
        try
        {
            _logger = logger;
            _screenCapture = screenCapture;
            _mouseProc = MouseHookProc;
            _keyboardProc = KeyboardHookProc;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize WindowsInputHook");
            // Continue initialization without throwing, hooks will fail gracefully later
        }
    }
    
    public void StartHook()
    {
        try
        {
            if (IsHooked)
            {
                _logger.LogError("Input hooks are already active");
                return;
            }
            
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule?.ModuleName == null)
            {
                throw new InvalidOperationException("Could not get current module information");
            }
            
            var moduleHandle = GetModuleHandle(curModule.ModuleName);
            
            // Install mouse hook
            _mouseHookId = SetWindowsHookEx(
                WH_MOUSE_LL,
                _mouseProc,
                moduleHandle,
                0);
            
            if (_mouseHookId == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to install mouse hook. Error: {error}");
            }
            
            // Install keyboard hook
            _keyboardHookId = SetWindowsHookEx(
                WH_KEYBOARD_LL,
                _keyboardProc,
                moduleHandle,
                0);
            
            if (_keyboardHookId == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
                throw new InvalidOperationException($"Failed to install keyboard hook. Error: {error}");
            }
            
            _logger.LogError("Input hooks installed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start input hooks");
            throw;
        }
    }
    
    public void StopHook()
    {
        try
        {
            if (_mouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }
            
            if (_keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookId);
                _keyboardHookId = IntPtr.Zero;
            }
            
            _logger.LogError("Input hooks removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop input hooks");
        }
    }
    
    public void SetStopKey(int virtualKeyCode)
    {
        _stopKey = virtualKeyCode;
    }
    
    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<POINT>(lParam);
                var timestamp = DateTime.UtcNow;
                
                var mouseEvent = (int)wParam switch
                {
                    WM_LBUTTONDOWN => new MouseInputEvent
                    {
                        Position = new Point(hookStruct.x, hookStruct.y),
                        Button = MouseButton.Left,
                        Action = MouseAction.Press,
                        Timestamp = timestamp
                    },
                    WM_LBUTTONUP => new MouseInputEvent
                    {
                        Position = new Point(hookStruct.x, hookStruct.y),
                        Button = MouseButton.Left,
                        Action = MouseAction.Release,
                        Timestamp = timestamp
                    },
                    WM_RBUTTONDOWN => new MouseInputEvent
                    {
                        Position = new Point(hookStruct.x, hookStruct.y),
                        Button = MouseButton.Right,
                        Action = MouseAction.Press,
                        Timestamp = timestamp
                    },
                    WM_RBUTTONUP => new MouseInputEvent
                    {
                        Position = new Point(hookStruct.x, hookStruct.y),
                        Button = MouseButton.Right,
                        Action = MouseAction.Release,
                        Timestamp = timestamp
                    },
                    WM_MBUTTONDOWN => new MouseInputEvent
                    {
                        Position = new Point(hookStruct.x, hookStruct.y),
                        Button = MouseButton.Middle,
                        Action = MouseAction.Press,
                        Timestamp = timestamp
                    },
                    WM_MBUTTONUP => new MouseInputEvent
                    {
                        Position = new Point(hookStruct.x, hookStruct.y),
                        Button = MouseButton.Middle,
                        Action = MouseAction.Release,
                        Timestamp = timestamp
                    },
                    _ => null
                };
                
                if (mouseEvent != null)
                {
                    // Capture screenshot asynchronously (R-004: within 50ms)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var screenshot = await _screenCapture.CaptureScreenAsync();
                            mouseEvent.ScreenshotData = screenshot;
                            
                            InputReceived?.Invoke(this, new InputEventArgs(mouseEvent));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to capture screenshot for mouse event");
                            InputReceived?.Invoke(this, new InputEventArgs(mouseEvent));
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mouse hook procedure");
        }
        
        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }
    
    private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                var timestamp = DateTime.UtcNow;
                
                // Check for stop key
                if (vkCode == _stopKey && ((int)wParam == WM_KEYDOWN || (int)wParam == WM_SYSKEYDOWN))
                {
                    InputReceived?.Invoke(this, new InputEventArgs(new StopKeyPressedEvent { Timestamp = timestamp }));
                    return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
                }
                
                var keyEvent = (int)wParam switch
                {
                    WM_KEYDOWN or WM_SYSKEYDOWN => new KeyboardInputEvent
                    {
                        VirtualKeyCode = vkCode,
                        Action = KeyAction.Press,
                        Timestamp = timestamp
                    },
                    WM_KEYUP or WM_SYSKEYUP => new KeyboardInputEvent
                    {
                        VirtualKeyCode = vkCode,
                        Action = KeyAction.Release,
                        Timestamp = timestamp
                    },
                    _ => null
                };
                
                if (keyEvent != null)
                {
                    // Capture screenshot for keyboard events too
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var screenshot = await _screenCapture.CaptureScreenAsync();
                            keyEvent.ScreenshotData = screenshot;
                            
                            InputReceived?.Invoke(this, new InputEventArgs(keyEvent));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to capture screenshot for keyboard event");
                            InputReceived?.Invoke(this, new InputEventArgs(keyEvent));
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in keyboard hook procedure");
        }
        
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }
    
    ~WindowsInputHook()
    {
        StopHook();
    }
    
    // Windows API declarations
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn,
        IntPtr hMod, uint dwThreadId);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
        IntPtr hMod, uint dwThreadId);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }
}

// Event classes
public class InputEventArgs : EventArgs
{
    public InputEvent InputEvent { get; }
    
    public InputEventArgs(InputEvent inputEvent)
    {
        InputEvent = inputEvent;
    }
}

public abstract class InputEvent
{
    public DateTime Timestamp { get; set; }
    public byte[]? ScreenshotData { get; set; }
}

public class MouseInputEvent : InputEvent
{
    public Point Position { get; set; }
    public MouseButton Button { get; set; }
    public MouseAction Action { get; set; }
    public int PressDownTimeMs { get; set; }
}

public class KeyboardInputEvent : InputEvent
{
    public int VirtualKeyCode { get; set; }
    public KeyAction Action { get; set; }
}

public class StopKeyPressedEvent : InputEvent
{
    // Special event to signal recording should stop
}