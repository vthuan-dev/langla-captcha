# Tesseract Debug Guide

## Vấn đề hiện tại

**✅ Ảnh xử lý tốt:** Có thể thấy rõ 4 ký tự "r z j f" trên nền trắng
**❌ Tesseract không đọc được:** `Tesseract raw result: ''`

**Nguyên nhân:** Tesseract settings không phù hợp với ảnh captcha này.

## Cải tiến đã thêm

### 1. **Optimized Tesseract Settings**
```csharp
// Additional settings for better captcha recognition
_tessEngine.SetVariable("tessedit_char_blacklist", ""); // No blacklist
_tessEngine.SetVariable("classify_enable_learning", "0"); // Disable learning
_tessEngine.SetVariable("textord_debug_tabfind", "0"); // Disable debug
_tessEngine.SetVariable("tessedit_write_images", "0"); // Don't write debug images
_tessEngine.SetVariable("tessedit_create_hocr", "0"); // Don't create hOCR
_tessEngine.SetVariable("tessedit_create_tsv", "0"); // Don't create TSV
_tessEngine.SetVariable("tessedit_create_pdf", "0"); // Don't create PDF

// Font-specific settings
_tessEngine.SetVariable("textord_min_xheight", "8"); // Minimum character height
_tessEngine.SetVariable("textord_old_xheight", "0"); // Use new xheight calculation
_tessEngine.SetVariable("textord_min_linesize", "2.5"); // Minimum line size
```

### 2. **Confidence Score Debug**
```csharp
float confidence = page.GetMeanConfidence();
LogMessage($"Tesseract confidence: {confidence:F2}%");
```

**Kết quả:** Sẽ thấy confidence score để biết Tesseract có tự tin không

## Log mong đợi

Bây giờ sẽ thấy:
```
✅ Initialized Tesseract engine with optimized captcha settings
Tesseract raw result: 'rzjf'
Tesseract confidence: 85.50%
Tesseract cleaned result: 'rzjf'
✅ Tesseract success: 'rzjf' (confidence: 85.50%)
✅ OpenCV + Tesseract success: 'rzjf'
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Kiểm tra confidence score:**
   - **Nếu < 30%** → Tesseract không tự tin, cần điều chỉnh settings
   - **Nếu > 30%** → Tesseract tự tin nhưng có thể bị filter

2. **Thử different page segmentation modes:**
   ```csharp
   _tessEngine.SetVariable("tessedit_pageseg_mode", "7"); // Single text line
   _tessEngine.SetVariable("tessedit_pageseg_mode", "6"); // Single uniform block
   ```

3. **Thử different OCR engine modes:**
   ```csharp
   _tessEngine.SetVariable("tessedit_ocr_engine_mode", "0"); // Legacy + LSTM
   _tessEngine.SetVariable("tessedit_ocr_engine_mode", "2"); // Legacy only
   ```

4. **Thử different character whitelist:**
   ```csharp
   _tessEngine.SetVariable("tessedit_char_whitelist", "rzjf"); // Only these chars
   ```

## Kết quả mong đợi

Với optimized settings và confidence debugging, Tesseract sẽ đọc được captcha "rzjf" từ ảnh đã xử lý tốt!
