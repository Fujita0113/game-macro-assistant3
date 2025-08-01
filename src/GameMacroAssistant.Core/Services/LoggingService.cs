using System.Text.Json;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface ILoggingService : ILogger
{
    Task LogAsync(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null);
    Task LogErrorAsync(string errorCode, string message, string? macroId = null, string? stepId = null, Exception? exception = null);
    Task<List<LogEntry>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, LogLevel? minLevel = null);
    Task ClearOldLogsAsync(TimeSpan maxAge);
}

public class LoggingService : ILoggingService
{
    private readonly string _logDirectory;
    private readonly object _lockObject = new();
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    
    public LoggingService()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "GameMacroAssistant", "Logs");
            
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        catch (Exception ex)
        {
            // Fallback to temp directory if AppData is not accessible
            _logDirectory = Path.Combine(Path.GetTempPath(), "GameMacroAssistant", "Logs");
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch
            {
                // If all fails, use in-memory logging only
                _logDirectory = string.Empty;
            }
        }
    }
    
    public async Task LogAsync(LogLevel level, string category, string message, Exception? exception = null, Dictionary<string, object>? properties = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Category = category,
            Message = message,
            Exception = exception?.ToString(),
            Properties = properties ?? new Dictionary<string, object>()
        };
        
        await WriteLogEntryAsync(logEntry);
    }
    
    public async Task LogErrorAsync(string errorCode, string message, string? macroId = null, string? stepId = null, Exception? exception = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Category = "Error",
            Message = message,
            Exception = exception?.ToString(),
            ErrorCode = errorCode,
            MacroId = macroId,
            StepId = stepId,
            Properties = new Dictionary<string, object>()
        };
        
        if (!string.IsNullOrEmpty(macroId))
            logEntry.Properties["macroId"] = macroId;
        if (!string.IsNullOrEmpty(stepId))
            logEntry.Properties["stepId"] = stepId;
        
        await WriteLogEntryAsync(logEntry);
        
        // Also write to console for development
        Console.WriteLine($"[{errorCode}] {message}");
        if (exception != null)
            Console.WriteLine(exception.ToString());
    }
    
    public void LogError(Exception? exception, string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        _ = Task.Run(() => LogAsync(LogLevel.Error, "General", formattedMessage, exception));
    }
    
    public void LogError(string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        _ = Task.Run(() => LogAsync(LogLevel.Error, "General", formattedMessage));
    }
    
    public void LogWarning(Exception? exception, string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        _ = Task.Run(() => LogAsync(LogLevel.Warning, "General", formattedMessage, exception));
    }
    
    public void LogWarning(string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        _ = Task.Run(() => LogAsync(LogLevel.Warning, "General", formattedMessage));
    }
    
    public void LogInformation(string message, params object[] args)
    {
        var formattedMessage = string.Format(message, args);
        _ = Task.Run(() => LogAsync(LogLevel.Information, "General", formattedMessage));
    }
    
    public async Task<List<LogEntry>> GetLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, LogLevel? minLevel = null)
    {
        var logs = new List<LogEntry>();
        
        try
        {
            var files = Directory.GetFiles(_logDirectory, "*.log")
                                .OrderByDescending(f => f);
            
            foreach (var file in files)
            {
                var fileLogs = await ReadLogFileAsync(file);
                logs.AddRange(fileLogs);
            }
            
            // Apply filters
            var filteredLogs = logs.AsEnumerable();
            
            if (fromDate.HasValue)
                filteredLogs = filteredLogs.Where(l => l.Timestamp >= fromDate.Value);
            
            if (toDate.HasValue)
                filteredLogs = filteredLogs.Where(l => l.Timestamp <= toDate.Value);
            
            if (minLevel.HasValue)
                filteredLogs = filteredLogs.Where(l => l.Level >= minLevel.Value);
            
            return filteredLogs.OrderByDescending(l => l.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read logs: {ex.Message}");
            return new List<LogEntry>();
        }
    }
    
    public async Task ClearOldLogsAsync(TimeSpan maxAge)
    {
        try
        {
            if (string.IsNullOrEmpty(_logDirectory) || !Directory.Exists(_logDirectory))
                return;
                
            var cutoffDate = DateTime.UtcNow - maxAge;
            var files = Directory.GetFiles(_logDirectory, "*.log");
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffDate)
                {
                    File.Delete(file);
                    Console.WriteLine($"Deleted old log file: {Path.GetFileName(file)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear old logs: {ex.Message}");
        }
        
        await Task.CompletedTask;
    }
    
    private async Task WriteLogEntryAsync(LogEntry logEntry)
    {
        await _fileSemaphore.WaitAsync();
        
        try
        {
            // Skip file logging if directory is not available
            if (string.IsNullOrEmpty(_logDirectory))
            {
                Console.WriteLine($"[{logEntry.Level}] {logEntry.Category}: {logEntry.Message}");
                return;
            }
            
            var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
            var filePath = Path.Combine(_logDirectory, fileName);
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true
            };
            
            var jsonLine = JsonSerializer.Serialize(logEntry, options);
            
            await File.AppendAllTextAsync(filePath, jsonLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Fallback to console if file logging fails
            Console.WriteLine($"Failed to write log: {ex.Message}");
            Console.WriteLine($"[{logEntry.Level}] {logEntry.Category}: {logEntry.Message}");
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }
    
    private async Task<List<LogEntry>> ReadLogFileAsync(string filePath)
    {
        var logs = new List<LogEntry>();
        
        try
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var logEntry = JsonSerializer.Deserialize<LogEntry>(line, options);
                    if (logEntry != null)
                    {
                        logs.Add(logEntry);
                    }
                }
                catch (JsonException)
                {
                    // Skip invalid JSON lines
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read log file {filePath}: {ex.Message}");
        }
        
        return logs;
    }
    
    // Background task to automatically clean old logs
    public async Task StartLogCleanupTask()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await ClearOldLogsAsync(TimeSpan.FromDays(30));
                    await Task.Delay(TimeSpan.FromDays(1)); // Check daily
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Log cleanup task error: {ex.Message}");
                    await Task.Delay(TimeSpan.FromHours(1)); // Retry in an hour
                }
            }
        });
    }
}