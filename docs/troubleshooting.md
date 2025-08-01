# Troubleshooting Guide

## DPI Scaling Issues (R-022)

### Known Limitations

Game Macro Assistant has known limitations when running on systems with DPI scaling other than 100%. This affects coordinate accuracy for recorded and executed macros.

**Affected Operations:**
- Mouse click coordinates may be offset
- Screen capture regions may be incorrect
- Image matching may fail due to size mismatches

**Supported DPI Settings:**
- ✅ 100% (Recommended)
- ⚠️ 125% (Partial support with coordinate transformation)
- ⚠️ 150% (Partial support with coordinate transformation)
- ❌ 200%+ (Not recommended)

### Coordinate Transformation Algorithm

When DPI scaling is detected, the following transformation is applied:

```pseudocode
function TransformCoordinates(logicalX, logicalY, dpiScale):
    physicalX = logicalX * dpiScale
    physicalY = logicalY * dpiScale
    return (physicalX, physicalY)

function GetDpiScale():
    userDpi = GetSystemDpi()
    standardDpi = 96  // Windows standard DPI
    return userDpi / standardDpi

// Example usage
dpiScale = GetDpiScale()
if (dpiScale != 1.0):
    recordedCoords = TransformCoordinates(rawX, rawY, 1.0 / dpiScale)
    playbackCoords = TransformCoordinates(recordedCoords.x, recordedCoords.y, dpiScale)
```

### Workarounds

1. **Set Display to 100% Scaling** (Recommended)
   - Right-click Desktop → Display Settings
   - Set "Scale and layout" to 100%
   - Restart the application

2. **Per-Monitor DPI Awareness**
   - Add the following to your application manifest:
   ```xml
   <application xmlns="urn:schemas-microsoft-com:asm.v3">
     <windowsSettings>
       <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
       <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
     </windowsSettings>
   </application>
   ```

## Screen Capture Issues

### Desktop Duplication API Failures

**Error Code:** `Err-CAP`

**Symptoms:**
- Black screenshots
- "CaptureLimited" watermark appears
- Performance degradation

**Causes:**
- Exclusive fullscreen applications
- Protected content (DRM)
- Graphics driver issues
- Insufficient permissions

**Solutions:**
1. Run application as Administrator
2. Update graphics drivers
3. Switch games to windowed/borderless mode
4. Disable hardware acceleration in problematic applications

### GDI BitBlt Fallback

When Desktop Duplication API fails, the application automatically falls back to GDI BitBlt with the following limitations:

- Maximum 15 FPS capture rate
- "CaptureLimited" watermark overlay
- Higher CPU usage
- May not capture protected content

## Performance Issues

### High CPU/Memory Usage

**Error Indicators:**
- Red progress bars
- "High Load" toast notifications
- System responsiveness issues

**Thresholds (R-020):**
- CPU: >15%
- RAM: >300MB

**Solutions:**
1. Reduce image match frequency
2. Increase step delays
3. Close unnecessary applications
4. Lower screen capture resolution
5. Disable real-time antivirus scanning for macro files

### Timing Accuracy Issues

**Error Code:** `Err-TIM`

**Thresholds (R-014):**
- Average timing error: ≤5ms
- Maximum timing error: ≤15ms

**Causes:**
- System under high load
- Background processes interfering
- Timer resolution limitations

**Solutions:**
1. Close resource-intensive applications
2. Set application to "High" priority in Task Manager
3. Disable Windows Game Mode
4. Use dedicated gaming mode/power profile

## Image Matching Problems

**Error Code:** `Err-MATCH`

### Common Issues

1. **False Negatives** (Image not found when it should be)
   - Lower the SSIM threshold (default: 0.95)
   - Increase pixel difference tolerance
   - Capture larger reference regions
   - Avoid areas with animations/changing content

2. **False Positives** (Wrong image matches)
   - Increase SSIM threshold
   - Reduce pixel difference tolerance
   - Use more unique reference images
   - Narrow the search region

### Threshold Tuning (R-013)

**SSIM Threshold:** 0.50 - 1.00 (Default: 0.95)
- Higher = More strict matching
- Lower = More permissive matching

**Pixel Difference:** 0% - 10% (Default: 3%)
- Higher = More tolerance for color variations
- Lower = Stricter color matching

## Encryption and Security

### Passphrase Requirements (R-018)

- Minimum 8 characters
- Maximum 3 failed attempts before cancellation
- Case-sensitive

**Error Scenarios:**
1. **Weak Passphrase**
   - Solution: Use at least 8 characters with mix of letters, numbers, symbols

2. **Forgotten Passphrase**
   - No recovery option available
   - Macro file will be inaccessible
   - Prevention: Use password manager

3. **Repeated Failures**
   - Application cancels load operation after 3 attempts
   - Solution: Restart application to retry

## Global Hotkey Conflicts (R-012)

### Conflict Resolution

When a hotkey is already in use:

1. Application detects conflict during registration
2. Presents 3 alternative suggestions:
   - Primary suggestion (adjacent key)
   - Secondary suggestion (different modifier)
   - Tertiary suggestion (different key combination)
3. User must select and save before hotkey is registered

**Common Conflicts:**
- F9: Default macro execution (may conflict with game functions)
- F5: Browser refresh
- Ctrl+C/V: System clipboard
- Windows key combinations: System shortcuts

### Recommended Hotkeys

**Low Conflict Risk:**
- F13-F24 (if available)
- Ctrl+Alt+[Letter]
- Shift+Alt+[Letter]

**High Conflict Risk:**
- F1-F12 (game functions)
- Ctrl+[Letter] (applications)
- Alt+[Letter] (menu access)

## Logging and Diagnostics

### Log File Location
```
%APPDATA%\GameMacroAssistant\Logs\YYYY-MM-DD.log
```

### Log Format (JSON)
```json
{
  "timestamp": "2024-01-15T10:30:00.000Z",
  "level": "Error",
  "category": "ScreenCapture",
  "message": "Desktop Duplication API failed",
  "errorCode": "Err-CAP",
  "macroId": "abc123",
  "stepId": "step456",
  "exception": "System.Exception: Access denied",
  "properties": {
    "attemptNumber": 2,
    "fallbackUsed": true
  }
}
```

### Common Log Entries

- `Err-CAP`: Screen capture failures
- `Err-TIM`: Timing accuracy violations  
- `Err-MATCH`: Image matching failures
- `Err-INPUT`: Input simulation failures
- `Err-CRYPTO`: Encryption/decryption errors
- `Err-IO`: File system errors

## Getting Help

1. **Check Log Files** first for error codes and details
2. **Review Requirements** in your target applications
3. **Test with Simple Macros** to isolate issues
4. **Contact Support** with log files and system information

### System Information to Include

- Windows version and build
- Display configuration (resolution, DPI scaling)
- Graphics card and driver version
- Target application details
- Relevant log entries with timestamps