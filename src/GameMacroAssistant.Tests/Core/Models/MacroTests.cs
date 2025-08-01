using GameMacroAssistant.Core.Models;
using Xunit;

namespace GameMacroAssistant.Tests.Core.Models;

public class MacroTests
{
    [Fact]
    public void Macro_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var macro = new Macro();
        
        // Assert
        Assert.NotNull(macro.Id);
        Assert.NotEmpty(macro.Id);
        Assert.Equal(string.Empty, macro.Name);
        Assert.Equal(string.Empty, macro.Description);
        Assert.Equal("1.0", macro.SchemaVersion);
        Assert.NotNull(macro.Steps);
        Assert.Empty(macro.Steps);
        Assert.NotNull(macro.Settings);
        Assert.False(macro.IsEncrypted);
        Assert.Null(macro.PassphraseHash);
    }
    
    [Fact]
    public void MacroSettings_DefaultValues_ShouldMatchRequirements()
    {
        // Arrange & Act
        var settings = new MacroSettings();
        
        // Assert - R-013: Default SSIM 0.95, pixel diff 3%
        Assert.Equal(0.95, settings.ImageMatchThreshold);
        Assert.Equal(0.03, settings.PixelDifferenceThreshold);
        Assert.Equal(5000, settings.TimeoutMs);
        Assert.Equal("F9", settings.GlobalHotkey);
        Assert.Equal(3, settings.MaxRetries);
    }
    
    [Fact]
    public void Macro_AddSteps_ShouldMaintainOrder()
    {
        // Arrange
        var macro = new Macro();
        var step1 = new DelayStep { Order = 0, DelayMs = 100 };
        var step2 = new DelayStep { Order = 1, DelayMs = 200 };
        
        // Act
        macro.Steps.Add(step1);
        macro.Steps.Add(step2);
        
        // Assert
        Assert.Equal(2, macro.Steps.Count);
        Assert.Equal(step1, macro.Steps[0]);
        Assert.Equal(step2, macro.Steps[1]);
    }
}