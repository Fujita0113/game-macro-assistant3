using System.ComponentModel.DataAnnotations;

namespace GameMacroAssistant.Core.Models;

public class Macro
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    public string SchemaVersion { get; set; } = "1.0";
    
    public List<Step> Steps { get; set; } = new();
    
    public MacroSettings Settings { get; set; } = new();
    
    public bool IsEncrypted { get; set; }
    
    public string? PassphraseHash { get; set; }
}

public class MacroSettings
{
    public double ImageMatchThreshold { get; set; } = 0.95;
    
    public double PixelDifferenceThreshold { get; set; } = 0.03;
    
    public int TimeoutMs { get; set; } = 5000;
    
    public string GlobalHotkey { get; set; } = "F9";
    
    public int MaxRetries { get; set; } = 3;
}