namespace GameMacroAssistant.Core.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public LogLevel Level { get; set; }
    
    public string Category { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string? Exception { get; set; }
    
    public Dictionary<string, object> Properties { get; set; } = new();
    
    public string? ErrorCode { get; set; }
    
    public string? MacroId { get; set; }
    
    public string? StepId { get; set; }
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public static class ErrorCodes
{
    public const string ERR_CAP = "Err-CAP";
    public const string ERR_TIM = "Err-TIM";
    public const string ERR_MATCH = "Err-MATCH";
    public const string ERR_INPUT = "Err-INPUT";
    public const string ERR_CRYPTO = "Err-CRYPTO";
    public const string ERR_IO = "Err-IO";
}