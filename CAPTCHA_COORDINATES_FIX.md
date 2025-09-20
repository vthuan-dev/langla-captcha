# Captcha Coordinates Fix

## Problem Identified

The captcha capture was failing with a 1x1 pixel image because of incorrect coordinate handling. The logs showed:

```
🔍 Image analysis: Size=1x1, PixelFormat=Format32bppArgb
🎨 Dominant color: Color [A=0, R=0, G=0, B=0] (appears 1 times)
⚠️ WARNING: Image appears to be mostly solid color - may not contain captcha text!
```

## Root Cause

1. **Coordinate System Confusion**: The config had `UseManualCapture: true` and `UseRelativeCoordinates: true`, but the manual capture coordinates (500, 200) were being treated as screen coordinates instead of client coordinates.

2. **Incorrect Coordinate Conversion**: The system was not properly converting client coordinates to screen coordinates when using manual capture with relative coordinates.

3. **Wrong Coordinate Values**: The original coordinates (500, 200) were too far from the game window bounds, resulting in invalid capture areas.

## Fixes Applied

### 1. Fixed Coordinate Conversion Logic in MainForm.cs

**Before:**
```csharp
// Priority 0: Manual Capture (screen coords)
if (cfg.UseManualCapture)
{
    // Return coordinates as-is without proper conversion
    return _manualCapture.CaptchaArea;
}
```

**After:**
```csharp
// Priority 0: Manual Capture (client coords, converted to screen if needed)
if (cfg.UseManualCapture)
{
    // If using absolute coordinates, return as-is (screen coords)
    if (cfg.UseAbsoluteCoordinates)
    {
        return _manualCapture.CaptchaArea;
    }
    
    // If using relative coordinates, convert client coords to screen coords
    if (_selectedGameWindow != null && _selectedGameWindow.IsValid())
    {
        var clientArea = _manualCapture.CaptchaArea;
        var windowBounds = _selectedGameWindow.Bounds;
        var screenArea = new Rectangle(
            windowBounds.X + clientArea.X,
            windowBounds.Y + clientArea.Y,
            clientArea.Width,
            clientArea.Height);
        return screenArea;
    }
}
```

### 2. Fixed Cropping Logic

**Before:**
```csharp
if (cfg.UseManualCapture || cfg.UseAbsoluteCoordinates)
{
    // Both treated the same way
}
```

**After:**
```csharp
if (cfg.UseAbsoluteCoordinates)
{
    // Handle absolute screen coordinates
}
else if (cfg.UseManualCapture)
{
    // Handle manual capture with proper coordinate conversion
}
```

### 3. Updated Config Coordinates

**Before:**
```json
{
  "ManualCaptchaArea": {
    "X": 500,
    "Y": 200,
    "Width": 100,
    "Height": 50
  }
}
```

**After:**
```json
{
  "ManualCaptchaArea": {
    "X": 50,
    "Y": 50,
    "Width": 200,
    "Height": 80
  }
}
```

## Expected Results

After these fixes:

1. **Proper Coordinate Conversion**: Manual capture coordinates are now correctly converted from client coordinates to screen coordinates.

2. **Valid Capture Area**: The coordinates (50, 50) with size (200, 80) should be within the game window bounds.

3. **Better OCR Results**: The larger capture area (200x80 vs 100x50) should provide better OCR accuracy.

4. **Debug Information**: The logs should now show proper coordinate conversion and valid image sizes.

## Testing

To test the fix:

1. Run the application
2. Select the game window
3. Try the "One-shot" captcha capture
4. Check the debug images in `captcha_debug/` folder
5. Verify the captured image is not 1x1 pixels and contains actual captcha content

## Additional Recommendations

1. **Use Manual Capture Setup**: Use the "Set Captcha Area" button to visually select the correct captcha area.

2. **Enable Auto-Detection**: Consider setting `"AutoDetectCaptchaArea": true` for automatic captcha detection.

3. **Monitor Debug Images**: Check the saved debug images to ensure proper capture.

4. **Adjust Coordinates**: If the captcha area is still not correct, adjust the coordinates in config.json based on the actual game window layout.