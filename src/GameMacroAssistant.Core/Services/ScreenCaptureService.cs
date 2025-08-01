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
    private const int GDI_MIN_INTERVAL_MS = 67; // ~15 FPS limit as per R-006
    private DateTime _lastGdiCapture = DateTime.MinValue;
    
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
        try
        {
            // Desktop Duplication API implementation
            // Note: This is a simplified implementation focusing on the core functionality
            // Full Desktop Duplication API requires more complex DXGI setup
            
            await Task.Delay(1); // Minimal async compliance
            
            // For now, fall back to GDI as Desktop Duplication API requires
            // complex DirectX/DXGI initialization that would need additional dependencies
            // This is marked for future enhancement when DXGI packages are added
            
            _logger.LogWarning("Desktop Duplication API not fully implemented, falling back to GDI");
            _useDesktopDuplication = false; // Disable for this session
            
            return null; // Will trigger GDI fallback
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Desktop Duplication API failed");
            _useDesktopDuplication = false;
            return null;
        }
    }
    
    private async Task<byte[]?> CaptureWithGdiAsync(Rectangle region)
    {
        // Enforce 15 FPS limit as per R-006
        var now = DateTime.UtcNow;
        var timeSinceLastCapture = now - _lastGdiCapture;
        if (timeSinceLastCapture.TotalMilliseconds < GDI_MIN_INTERVAL_MS)
        {
            var delayMs = GDI_MIN_INTERVAL_MS - (int)timeSinceLastCapture.TotalMilliseconds;
            await Task.Delay(delayMs);
        }
        
        _lastGdiCapture = DateTime.UtcNow;
        
        try
        {
            using var bitmap = new Bitmap(region.Width, region.Height);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Capture screen region using GDI BitBlt
            graphics.CopyFromScreen(region.Location, Point.Empty, region.Size);
            
            // Add "CaptureLimited" watermark overlay as per R-006
            AddWatermark(graphics, region.Size);
            
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GDI screen capture failed for region {Region}", region);
            throw; // Re-throw to trigger retry logic in parent method
        }
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

public static class Screen
{
    public static ScreenInfo? PrimaryScreen 
    {
        get
        {
            // Get actual screen dimensions using Windows API
            var width = GetSystemMetrics(SM_CXSCREEN);
            var height = GetSystemMetrics(SM_CYSCREEN);
            return new ScreenInfo { Bounds = new Rectangle(0, 0, width, height) };
        }
    }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}

public class ScreenInfo
{
    public Rectangle Bounds { get; set; }
}