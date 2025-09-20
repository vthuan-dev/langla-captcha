# Tesseract Settings Debug

## Vấn đề hiện tại

**✅ OpenCV xử lý tốt:** `✅ Upscaled normal image by 8x: 200x80 -> 1600x640`
**❌ Tesseract không đọc được:** `Tesseract raw result: ''` - Cả ảnh gốc và inverted đều không đọc được

## Nguyên nhân

**Tesseract không đọc được ảnh trắng đen** từ OpenCV. Có thể do:
1. **Ảnh có noise quá nhiều** sau khi upscale
2. **Tesseract settings không phù hợp** với ảnh này
3. **Ảnh có background phức tạp** (màu nâu: R=134, G=66, B=25)

## Cải tiến đã thêm

### 1. **Different Tesseract Settings**
```csharp
// Try different settings for captcha images
_tessEngine.SetVariable("tessedit_pageseg_mode", "7"); // Single text line (changed from 8)
_tessEngine.SetVariable("tessedit_ocr_engine_mode", "0"); // Legacy + LSTM (changed from 1)
```

**Kết quả:** Thử different page segmentation và OCR engine modes

### 2. **Debug Image Content**
```csharp
// Count black pixels (should be text)
int blackPixels = 0;
for (int y = 0; y < processedBitmap.Height; y += 10) // Sample every 10 pixels
{
    for (int x = 0; x < processedBitmap.Width; x += 10)
    {
        var pixel = processedBitmap.GetPixel(x, y);
        if (pixel.R < 50 && pixel.G < 50 && pixel.B < 50) // Dark pixel
        {
            blackPixels++;
        }
    }
}
LogMessage($"🔍 Debug: Found {blackPixels} dark pixels (sampled every 10px)");
```

**Kết quả:** Kiểm tra ảnh có text (dark pixels) không

## Log mong đợi

Bây giờ sẽ thấy:
```
✅ Upscaled normal image by 8x: 200x80 -> 1600x640
🔍 Debug: Image size: 1600x640
🔍 Debug: Found 150 dark pixels (sampled every 10px)
🔍 Trying normal image...
Tesseract raw result: 'abc123'
Tesseract confidence: 15.50%
✅ Tesseract success: 'abc123' (confidence: 15.50%)
✅ OpenCV + Tesseract success: 'abc123'
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Kiểm tra dark pixels:**
   - **Nếu < 10 pixels** → Ảnh không có text, vấn đề ở OpenCV
   - **Nếu > 50 pixels** → Ảnh có text, vấn đề ở Tesseract

2. **Thử different page segmentation modes:**
   ```csharp
   _tessEngine.SetVariable("tessedit_pageseg_mode", "6"); // Single uniform block
   _tessEngine.SetVariable("tessedit_pageseg_mode", "13"); // Raw line. Treat the image as a single text line
   ```

3. **Thử different OCR engine modes:**
   ```csharp
   _tessEngine.SetVariable("tessedit_ocr_engine_mode", "2"); // Legacy only
   ```

4. **Thử different character whitelist:**
   ```csharp
   _tessEngine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz"); // Only lowercase
   ```

## Kết quả mong đợi

Với different Tesseract settings và debug image content, Tesseract sẽ đọc được captcha từ ảnh trắng đen!
