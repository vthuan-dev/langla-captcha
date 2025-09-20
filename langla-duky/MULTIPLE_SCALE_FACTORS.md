# Multiple Scale Factors để tối ưu Tesseract

## Vấn đề hiện tại

Từ log phân tích:
- **AutoDetectCaptchaArea hoạt động tốt** ✅ (200x80, 278 non-white pixels)
- **OpenCV xử lý tốt** ✅ (threshold 180, 802 non-white pixels, upscale 4x)
- **Tesseract vẫn không đọc được** ❌ (tất cả PSM modes trả về empty, confidence 0.95%)

## Nguyên nhân

**Tesseract không nhận diện được text từ ảnh đã xử lý OpenCV!** Có thể do:

1. **Ảnh quá lớn** (800x320) - Tesseract thích ảnh nhỏ hơn
2. **Text quá mờ** sau khi xử lý
3. **Cần thử different scale factors**

## Giải pháp: Multiple Scale Factors

### **1. Thử nhiều scale factors**
```csharp
float[] scaleFactors = { 2.0f, 3.0f, 4.0f, 6.0f }; // Different scale factors

foreach (float scaleFactor in scaleFactors)
{
    LogMessage($"🔍 Trying scale factor {scaleFactor}x...");
    
    // Upscale image
    Cv2.Resize(matBinary, matUpscaled, new OpenCvSharp.Size(
        (int)(matBinary.Width * scaleFactor), 
        (int)(matBinary.Height * scaleFactor)), 
        0, 0, InterpolationFlags.Cubic);
    
    // Try OCR
    result = ProcessWithTesseractMultiplePSM(processedBitmap);
    
    if (!string.IsNullOrEmpty(result))
    {
        LogMessage($"✅ Found result with scale {scaleFactor}x: '{result}'");
        break; // Exit the scale factor loop
    }
}
```

### **2. Kích thước ảnh tương ứng**
- **2x**: 200x80 → 400x160
- **3x**: 200x80 → 600x240  
- **4x**: 200x80 → 800x320
- **6x**: 200x80 → 1200x480

### **3. Thử cả normal và inverted images**
```csharp
// Try normal image first
result = ProcessWithTesseractMultiplePSM(processedBitmap);

// If normal image fails, try inverted image
if (string.IsNullOrEmpty(result))
{
    using var matInverted = new Mat();
    Cv2.BitwiseNot(matBinary, matInverted);
    
    // Try inverted image with multiple scale factors
    foreach (float scaleFactor in scaleFactors)
    {
        result = ProcessWithTesseractMultiplePSM(invertedBitmap);
        if (!string.IsNullOrEmpty(result)) break;
    }
}
```

## Log mong đợi

Bây giờ sẽ thấy:
```
🔍 Trying scale factor 2x...
✅ Upscaled normal image by 2x: 200x80 -> 400x160
🔍 Trying normal image with multiple PSM modes (scale 2x)...
🔍 Trying PSM mode 6...
PSM 6: raw='rzjf', confidence=85.50%
✅ PSM 6 success: 'rzjf' (confidence: 85.50%)
✅ Found result with scale 2x: 'rzjf'
✅ OpenCV + Tesseract success: 'rzjf'
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Thử different scale factors:**
   ```csharp
   float[] scaleFactors = { 1.5f, 2.5f, 5.0f, 8.0f }; // Different values
   ```

2. **Thử different PSM modes:**
   ```csharp
   int[] psmModes = { 3, 4, 5, 10 }; // Different modes
   ```

3. **Thử different threshold values:**
   ```csharp
   int[] thresholds = { 120, 140, 160, 200 }; // Different thresholds
   ```

## Kết quả mong đợi

Với multiple scale factors, Tesseract sẽ tìm được scale factor tốt nhất để đọc captcha từ ảnh đã được OpenCV xử lý tối ưu!

**Tìm scale factor tối ưu cho Tesseract!** 🎯
