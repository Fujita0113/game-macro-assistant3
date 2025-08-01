using System.Drawing;
using System.Drawing.Imaging;
using GameMacroAssistant.Core.Models;

namespace GameMacroAssistant.Core.Services;

public interface IImageMatcher
{
    Task<ImageMatchResult> FindImageAsync(byte[] haystack, byte[] needle, Rectangle? searchRegion = null);
    Task<double> CalculateSimilarityAsync(byte[] image1, byte[] image2);
    void SetThresholds(double ssimThreshold, double pixelDifferenceThreshold);
}

public class ImageMatcher : IImageMatcher
{
    private double _ssimThreshold = 0.95;
    private double _pixelDifferenceThreshold = 0.03;
    
    public void SetThresholds(double ssimThreshold, double pixelDifferenceThreshold)
    {
        _ssimThreshold = ssimThreshold;
        _pixelDifferenceThreshold = pixelDifferenceThreshold;
    }
    
    public async Task<ImageMatchResult> FindImageAsync(byte[] haystack, byte[] needle, Rectangle? searchRegion = null)
    {
        return await Task.Run(() =>
        {
            using var haystackBitmap = LoadBitmap(haystack);
            using var needleBitmap = LoadBitmap(needle);
            
            if (haystackBitmap == null || needleBitmap == null)
            {
                return new ImageMatchResult { IsMatch = false, Confidence = 0.0 };
            }
            
            var region = searchRegion ?? new Rectangle(0, 0, haystackBitmap.Width, haystackBitmap.Height);
            
            // TODO: Implement template matching algorithm
            // Should use both SSIM and pixel difference calculations
            var bestMatch = FindBestMatch(haystackBitmap, needleBitmap, region);
            
            return bestMatch;
        });
    }
    
    public async Task<double> CalculateSimilarityAsync(byte[] image1, byte[] image2)
    {
        return await Task.Run(() =>
        {
            using var bitmap1 = LoadBitmap(image1);
            using var bitmap2 = LoadBitmap(image2);
            
            if (bitmap1 == null || bitmap2 == null || 
                bitmap1.Width != bitmap2.Width || bitmap1.Height != bitmap2.Height)
            {
                return 0.0;
            }
            
            // TODO: Implement SSIM calculation
            return CalculateSSIM(bitmap1, bitmap2);
        });
    }
    
    private Bitmap? LoadBitmap(byte[] imageData)
    {
        try
        {
            using var stream = new MemoryStream(imageData);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }
    
    private ImageMatchResult FindBestMatch(Bitmap haystack, Bitmap needle, Rectangle searchRegion)
    {
        // TODO: Implement template matching with sliding window
        // Calculate SSIM and pixel difference for each position
        // Return best match above threshold
        
        var bestConfidence = 0.0;
        var bestPosition = Point.Empty;
        
        // Simplified placeholder implementation
        for (int y = searchRegion.Y; y <= searchRegion.Bottom - needle.Height; y += 2)
        {
            for (int x = searchRegion.X; x <= searchRegion.Right - needle.Width; x += 2)
            {
                var confidence = CalculateMatchConfidence(haystack, needle, new Point(x, y));
                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;
                    bestPosition = new Point(x, y);
                }
            }
        }
        
        return new ImageMatchResult
        {
            IsMatch = bestConfidence >= _ssimThreshold,
            Confidence = bestConfidence,
            Location = bestPosition,
            SearchRegion = searchRegion
        };
    }
    
    private double CalculateMatchConfidence(Bitmap haystack, Bitmap needle, Point position)
    {
        if (position.X + needle.Width > haystack.Width || 
            position.Y + needle.Height > haystack.Height)
        {
            return 0.0;
        }
        
        // Extract region from haystack
        using var region = new Bitmap(needle.Width, needle.Height);
        using var graphics = Graphics.FromImage(region);
        
        var sourceRect = new Rectangle(position.X, position.Y, needle.Width, needle.Height);
        var destRect = new Rectangle(0, 0, needle.Width, needle.Height);
        
        graphics.DrawImage(haystack, destRect, sourceRect, GraphicsUnit.Pixel);
        
        // Calculate SSIM
        var ssim = CalculateSSIM(region, needle);
        
        // Calculate pixel difference percentage
        var pixelDiff = CalculatePixelDifference(region, needle);
        
        // Combine both metrics (weighted average)
        var combinedScore = (ssim * 0.7) + ((1.0 - pixelDiff) * 0.3);
        
        return Math.Max(0, Math.Min(1, combinedScore));
    }
    
    private double CalculatePixelDifference(Bitmap image1, Bitmap image2)
    {
        if (image1.Width != image2.Width || image1.Height != image2.Height)
            return 1.0; // 100% different
        
        int totalPixels = image1.Width * image1.Height;
        int differentPixels = 0;
        
        const int tolerance = 30; // RGB difference tolerance
        
        for (int y = 0; y < image1.Height; y++)
        {
            for (int x = 0; x < image1.Width; x++)
            {
                var pixel1 = image1.GetPixel(x, y);
                var pixel2 = image2.GetPixel(x, y);
                
                var diff = Math.Abs(pixel1.R - pixel2.R) +
                          Math.Abs(pixel1.G - pixel2.G) +
                          Math.Abs(pixel1.B - pixel2.B);
                
                if (diff > tolerance)
                {
                    differentPixels++;
                }
            }
        }
        
        return (double)differentPixels / totalPixels;
    }
    
    private double CalculateSSIM(Bitmap image1, Bitmap image2)
    {
        if (image1.Width != image2.Width || image1.Height != image2.Height)
            return 0.0;
        
        const double K1 = 0.01;
        const double K2 = 0.03;
        const double L = 255; // Dynamic range of pixel values
        
        const double C1 = (K1 * L) * (K1 * L);
        const double C2 = (K2 * L) * (K2 * L);
        
        double sum1 = 0, sum2 = 0;
        double sum1Sq = 0, sum2Sq = 0;
        double sum12 = 0;
        
        int totalPixels = image1.Width * image1.Height;
        
        // Convert to grayscale and calculate means
        for (int y = 0; y < image1.Height; y++)
        {
            for (int x = 0; x < image1.Width; x++)
            {
                var pixel1 = image1.GetPixel(x, y);
                var pixel2 = image2.GetPixel(x, y);
                
                // Convert to grayscale
                double gray1 = 0.299 * pixel1.R + 0.587 * pixel1.G + 0.114 * pixel1.B;
                double gray2 = 0.299 * pixel2.R + 0.587 * pixel2.G + 0.114 * pixel2.B;
                
                sum1 += gray1;
                sum2 += gray2;
                sum1Sq += gray1 * gray1;
                sum2Sq += gray2 * gray2;
                sum12 += gray1 * gray2;
            }
        }
        
        double mean1 = sum1 / totalPixels;
        double mean2 = sum2 / totalPixels;
        
        double variance1 = (sum1Sq / totalPixels) - (mean1 * mean1);
        double variance2 = (sum2Sq / totalPixels) - (mean2 * mean2);
        double covariance = (sum12 / totalPixels) - (mean1 * mean2);
        
        // SSIM calculation
        double numerator = (2 * mean1 * mean2 + C1) * (2 * covariance + C2);
        double denominator = (mean1 * mean1 + mean2 * mean2 + C1) * (variance1 + variance2 + C2);
        
        return Math.Max(0, Math.Min(1, numerator / denominator));
    }
}

public class ImageMatchResult
{
    public bool IsMatch { get; set; }
    public double Confidence { get; set; }
    public Point Location { get; set; }
    public Rectangle SearchRegion { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}