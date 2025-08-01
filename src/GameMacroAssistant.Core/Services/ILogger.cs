namespace GameMacroAssistant.Core.Services;

public interface ILogger
{
    void LogError(Exception? exception, string message, params object[] args);
    void LogError(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogInformation(string message, params object[] args);
}

public class ConsoleLogger : ILogger
{
    public void LogError(Exception? exception, string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        Console.WriteLine($"[ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {formattedMessage}");
        if (exception != null)
        {
            Console.WriteLine($"Exception: {exception.Message}");
        }
    }

    public void LogError(string message, params object[] args)
    {
        LogError(null, message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        Console.WriteLine($"[WARN] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {formattedMessage}");
    }

    public void LogInformation(string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        Console.WriteLine($"[INFO] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {formattedMessage}");
    }
}