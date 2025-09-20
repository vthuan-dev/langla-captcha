# Reduced Scale Fix

## Vấn đề hiện tại

**✅ OpenCV hoạt động hoàn hảo:**
- `✅ Upscaled normal image by 8x: 200x80 -> 1600x640`
- `🔍 Debug: Found 4323 dark pixels (sampled every 10px)`

**❌ Tesseract hoàn toàn không đọc được:**
- `Tesseract raw result: ''`
- `Tesseract raw result length: 0`
- `Tesseract raw result chars: []`

## Nguyên nhân

**Ảnh quá lớn** (1600x640) có thể làm Tesseract không xử lý được hoặc settings không phù hợp.

## Cải tiến đã thêm

### 1. **Reduced Scale Factor**
```csharp
float scaleFactor = 4.0f; // Scale up 4x for better OCR (reduced from 8x)
```

**Kết quả:** 200x80 → 800x320 (thay vì 1600x640)

### 2. **Different Tesseract Settings**
```csharp
_tessEngine.SetVariable("tessedit_pageseg_mode", "8"); // Single word (changed from 13)
_tessEngine.SetVariable("tessedit_ocr_engine_mode", "0"); // Legacy + LSTM (changed from 2)
```

**Kết quả:** Thử Single word mode và Legacy + LSTM engine

## Log mong đợi

Bây giờ sẽ thấy:
```
✅ Upscaled normal image by 4x: 200x80 -> 800x320
🔍 Debug: Found 1080 dark pixels (sampled every 10px)
🔍 Trying normal image...
Tesseract raw result: 'rzjf'
Tesseract confidence: 85.50%
Tesseract raw result length: 4
Tesseract raw result chars: ['r', 'z', 'j', 'f']
Tesseract cleaned result: 'rzjf'
✅ Tesseract success: 'rzjf' (confidence: 85.50%)
✅ OpenCV + Tesseract success: 'rzjf'
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Thử different scale factors:**
   ```csharp
   float scaleFactor = 2.0f; // Scale up 2x
   float scaleFactor = 6.0f; // Scale up 6x
   ```

2. **Thử different page segmentation modes:**
   ```csharp
   _tessEngine.SetVariable("tessedit_pageseg_mode", "6"); // Single uniform block
   _tessEngine.SetVariable("tessedit_pageseg_mode", "7"); // Single text line
   ```

3. **Thử different OCR engine modes:**
   ```csharp
   _tessEngine.SetVariable("tessedit_ocr_engine_mode", "1"); // LSTM only
   _tessEngine.SetVariable("tessedit_ocr_engine_mode", "2"); // Legacy only
   ```

## Kết quả mong đợi

Với reduced scale factor (4x) và Single word mode, Tesseract sẽ đọc được captcha từ ảnh 800x320 thay vì 1600x640 quá lớn!
