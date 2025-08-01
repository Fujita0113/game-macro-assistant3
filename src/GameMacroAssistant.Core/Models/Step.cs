using System.Drawing;

namespace GameMacroAssistant.Core.Models;

public abstract class Step
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public int Order { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? Description { get; set; }
    
    public abstract StepType Type { get; }
}

public class MouseStep : Step
{
    public override StepType Type => StepType.Mouse;
    
    public Point AbsolutePosition { get; set; }
    
    public MouseButton Button { get; set; }
    
    public MouseAction Action { get; set; }
    
    public int PressDownTimeMs { get; set; }
    
    public byte[]? ScreenshotData { get; set; }
    
    public Rectangle? ConditionRegion { get; set; }
}

public class KeyboardStep : Step
{
    public override StepType Type => StepType.Keyboard;
    
    public int VirtualKeyCode { get; set; }
    
    public KeyAction Action { get; set; }
    
    public DateTime PressTime { get; set; }
    
    public DateTime? ReleaseTime { get; set; }
    
    public byte[]? ScreenshotData { get; set; }
}

public class DelayStep : Step
{
    public override StepType Type => StepType.Delay;
    
    public int DelayMs { get; set; }
}

public class ConditionalStep : Step
{
    public override StepType Type => StepType.Conditional;
    
    public byte[] ConditionImage { get; set; } = Array.Empty<byte>();
    
    public Rectangle SearchRegion { get; set; }
    
    public double MatchThreshold { get; set; } = 0.95;
    
    public List<Step> OnMatchSteps { get; set; } = new();
    
    public List<Step> OnNoMatchSteps { get; set; } = new();
}

public enum StepType
{
    Mouse,
    Keyboard,
    Delay,
    Conditional
}

public enum MouseButton
{
    Left,
    Right,
    Middle,
    X1,
    X2
}

public enum MouseAction
{
    Press,
    Release,
    Click,
    DoubleClick,
    Move
}

public enum KeyAction
{
    Press,
    Release
}