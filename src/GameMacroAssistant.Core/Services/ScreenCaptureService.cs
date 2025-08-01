using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface IScreenCaptureService
{
    Task<byte[]?> CaptureScreenAsync();
    Task<byte[]?> CaptureRegionAsync(Rectangle region);
    bool IsDesktopDuplicationAvailable { get; }
    event EventHandler<CaptureErrorEventArgs>? CaptureError;
}

public class ScreenCaptureService : IScreenCaptureService
{
    private readonly ILogger _logger;
    private bool _useDesktopDuplication = true;
    private int _retryCount = 0;
    private const int MAX_RETRIES = 2;
    private const int BACKOFF_MS = 10;
    
    public bool IsDesktopDuplicationAvailable => _useDesktopDuplication;
    
    public event EventHandler<CaptureErrorEventArgs>? CaptureError;
    
    public ScreenCaptureService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task<byte[]?> CaptureScreenAsync()
    {
        var primaryScreen = Screen.PrimaryScreen;
        if (primaryScreen == null) return null;
        
        return await CaptureRegionAsync(primaryScreen.Bounds);
    }
    
    public async Task<byte[]?> CaptureRegionAsync(Rectangle region)
    {
        for (int attempt = 0; attempt <= MAX_RETRIES; attempt++)
        {
            try
            {
                if (_useDesktopDuplication)
                {
                    var result = await CaptureWithDesktopDuplicationAsync(region);
                    if (result != null)
                    {
                        _retryCount = 0;
                        return result;
                    }
                }
                
                // TODO: Fallback to GDI BitBlt with watermark overlay
                return await CaptureWithGdiAsync(region);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Screen capture failed on attempt {Attempt}", attempt + 1);
                
                if (attempt < MAX_RETRIES)
                {
                    await Task.Delay(BACKOFF_MS * (attempt + 1));
                }
            }
        }
        
        // Final failure - log error code
        _logger.LogError("Screen capture failed after {MaxRetries} attempts", MAX_RETRIES + 1);
        CaptureError?.Invoke(this, new(ErrorCodes.ERR_CAP, "Screen capture failed"));
        return null;
    }
    
    private async Task<byte[]?> CaptureWithDesktopDuplicationAsync(Rectangle region)
    {
        // TODO: Implement Desktop Duplication API capture
        // This requires Windows.Graphics.Capture or similar API
        await Task.Delay(1); // Placeholder
        return null;
    }
    
    private async Task<byte[]?> CaptureWithGdiAsync(Rectangle region)
    {
        // TODO: Implement GDI BitBlt fallback with watermark
        // Add "CaptureLimited" watermark overlay
        // Limit to 15 FPS maximum
        await Task.Delay(1); // Placeholder
        
        using var bitmap = new Bitmap(region.Width, region.Height);
        using var graphics = Graphics.FromImage(bitmap);
        
        // Capture screen region
        graphics.CopyFromScreen(region.Location, Point.Empty, region.Size);
        
        // Add watermark
        AddWatermark(graphics, region.Size);
        
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
    
    private void AddWatermark(Graphics graphics, Size imageSize)
    {
        const string watermarkText = "CaptureLimited";
        using var font = new Font("Arial", 12, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(128, 255, 255, 255));
        
        var textSize = graphics.MeasureString(watermarkText, font);
        var position = new PointF(
            imageSize.Width - textSize.Width - 10,
            imageSize.Height - textSize.Height - 10
        );
        
        graphics.DrawString(watermarkText, font, brush, position);
    }
}

public class CaptureErrorEventArgs : EventArgs
{
    public string ErrorCode { get; }
    public string Message { get; }
    
    public CaptureErrorEventArgs(string errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message;
    }
}

// Placeholder interfaces
public interface ILogger
{
    void LogError(Exception? exception, string message, params object[] args);
    void LogError(string message, params object[] args);
}

public static class Screen
{
    public static ScreenInfo? PrimaryScreen => new() { Bounds = new Rectangle(0, 0, 1920, 1080) };
}

public class ScreenInfo
{
    public Rectangle Bounds { get; set; }
}