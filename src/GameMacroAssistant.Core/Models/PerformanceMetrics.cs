namespace GameMacroAssistant.Core.Models;

public class PerformanceMetrics
{
    public double CpuUsagePercent { get; set; }
    
    public long MemoryUsageMB { get; set; }
    
    public TimeSpan ExecutionTime { get; set; }
    
    public double AverageTimingAccuracy { get; set; }
    
    public double MaxTimingAccuracy { get; set; }
    
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    
    public bool IsHighLoad => CpuUsagePercent > 15.0 || MemoryUsageMB > 300;
    
    public bool IsTimingAccurate => AverageTimingAccuracy <= 5.0 && MaxTimingAccuracy <= 15.0;
}