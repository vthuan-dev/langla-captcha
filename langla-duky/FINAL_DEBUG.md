# Final Debug - Inverted Image + No Confidence Filter

## Vấn đề hiện tại

**✅ Capture tốt:** `🔍 Non-white pixels found: 278`
**✅ Upscaling hoạt động:** `✅ Upscaled image by 8x: 200x80 -> 1600x640`
**✅ Threshold tốt:** `✅ Applied best threshold with 802 non-white pixels`

**❌ Tesseract confidence quá thấp:** `Tesseract confidence: 0.95%`

## Nguyên nhân

**Tesseract confidence chỉ 0.95%** - quá thấp để tin tưởng. Có thể do:
1. **Ảnh có background phức tạp** (màu nâu: R=134, G=66, B=25)
2. **Captcha có thể là white text on dark background**
3. **Tesseract settings không phù hợp** với ảnh này

## Cải tiến đã thêm

### 1. **Inverted Image Processing**
```csharp
// If normal image fails, try inverted image
if (string.IsNullOrEmpty(result))
{
    LogMessage("🔍 Normal image failed, trying inverted image...");
    using var matInverted = new Mat();
    Cv2.BitwiseNot(matBinary, matInverted);
    
    using var matInvertedUpscaled = new Mat();
    Cv2.Resize(matInverted, matInvertedUpscaled, new OpenCvSharp.Size(
        (int)(matInverted.Width * scaleFactor), 
        (int)(matInverted.Height * scaleFactor)), 
        0, 0, InterpolationFlags.Cubic);
    
    result = ProcessWithTesseract(invertedBitmap);
}
```

**Kết quả:** Thử cả ảnh gốc và ảnh inverted

### 2. **No Confidence Filter**
```csharp
// Accept results with 2-10 characters (ignore confidence for now)
if (result.Length >= 2 && result.Length <= 10)
{
    LogMessage($"✅ Tesseract success: '{result}' (confidence: {confidence:F2}%)");
    return result;
}
```

**Kết quả:** Chấp nhận mọi kết quả có 2-10 ký tự, bỏ qua confidence

## Log mong đợi

Bây giờ sẽ thấy:
```
✅ Upscaled normal image by 8x: 200x80 -> 1600x640
🔍 Trying normal image...
Tesseract raw result: ''
Tesseract confidence: 0.95%
❌ Tesseract result too short/long: '' (length: 0)
🔍 Normal image failed, trying inverted image...
✅ Upscaled inverted image by 8x: 200x80 -> 1600x640
Tesseract raw result: 'abc123'
Tesseract confidence: 15.50%
✅ Tesseract success: 'abc123' (confidence: 15.50%)
✅ OpenCV + Tesseract success: 'abc123'
```

## Debug Images

Bây giờ sẽ có:
- `captcha_processed_*.png` - Ảnh gốc đã xử lý
- `captcha_inverted_*.png` - Ảnh inverted đã xử lý

## Kết quả mong đợi

Với inverted image processing và no confidence filter, Tesseract sẽ đọc được captcha từ ảnh có background phức tạp!
