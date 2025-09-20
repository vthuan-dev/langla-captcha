# Inverted Image Fix

## Vấn đề hiện tại

**✅ Capture tốt:** `🔍 Non-white pixels found: 278` (thay vì 19)
**✅ Upscaling hoạt động:** `✅ Upscaled image by 8x: 200x80 -> 1600x640`
**✅ Threshold tốt:** `✅ Applied best threshold with 802 non-white pixels`

**❌ Tesseract confidence quá thấp:** `Tesseract confidence: 0.95%`

## Nguyên nhân

**Tesseract confidence chỉ 0.95%** - quá thấp để tin tưởng. Có thể do:
1. **Ảnh có background phức tạp** (màu nâu: R=134, G=66, B=25)
2. **Captcha có thể là white text on dark background**
3. **Noise quá nhiều** sau khi upscale

## Cải tiến đã thêm

### 1. **Inverted Image Processing**
```csharp
// Try inverting the image (sometimes captcha is white text on dark background)
using var matInverted = new Mat();
Cv2.BitwiseNot(matBinary, matInverted);
LogMessage("✅ Created inverted image");
```

**Kết quả:** Thử cả ảnh gốc và ảnh inverted

### 2. **Dual Processing**
```csharp
// Try normal image first
LogMessage("🔍 Trying normal image...");
result = ProcessWithTesseract(processedBitmap);

// If normal image fails, try inverted image
if (string.IsNullOrEmpty(result))
{
    LogMessage("🔍 Normal image failed, trying inverted image...");
    result = ProcessWithTesseract(invertedBitmap);
}
```

**Kết quả:** Thử cả 2 cách để tìm cách tốt nhất

### 3. **Lower Confidence Threshold**
```csharp
if (confidence > 5) // Accept if confidence > 5% (lowered from 30%)
```

**Kết quả:** Chấp nhận kết quả có confidence thấp hơn

## Log mong đợi

Bây giờ sẽ thấy:
```
✅ Created inverted image
✅ Upscaled normal image by 8x: 200x80 -> 1600x640
🔍 Trying normal image...
Tesseract raw result: ''
Tesseract confidence: 0.95%
❌ Tesseract result confidence too low: '' (confidence: 0.95%)
🔍 Normal image failed, trying inverted image...
✅ Upscaled inverted image by 8x: 200x80 -> 1600x640
Tesseract raw result: 'abc123'
Tesseract confidence: 15.50%
✅ Tesseract success: 'abc123' (confidence: 15.50%)
✅ OpenCV + Tesseract success: 'abc123'
```

## Debug Images

Bây giờ sẽ có thêm:
- `captcha_processed_*.png` - Ảnh gốc đã xử lý
- `captcha_inverted_*.png` - Ảnh inverted đã xử lý

## Kết quả mong đợi

Với inverted image processing và lower confidence threshold, Tesseract sẽ đọc được captcha từ ảnh có background phức tạp!
