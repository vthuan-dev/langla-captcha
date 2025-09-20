# Tesseract Optimization dựa trên Web Search

## Nguyên nhân Tesseract không đọc được ảnh OpenCV

### **1. Chất lượng hình ảnh không đủ tốt**
- Ảnh quá lớn (1600x640) hoặc quá nhỏ
- Độ phân giải không phù hợp với Tesseract

### **2. Xử lý trước hình ảnh chưa tối ưu**
- Thiếu `cv2.medianBlur` để loại bỏ nhiễu
- Nhị phân hóa ảnh chưa đủ tối ưu

### **3. Cấu hình Tesseract chưa phù hợp**
- Page Segmentation Mode (PSM) cần điều chỉnh
- OCR Engine Mode (OEM) có thể không phù hợp

## Cải tiến đã thêm

### **1. Enhanced Noise Reduction**
```csharp
// Additional noise reduction with median blur
using var matBlurred = new Mat();
Cv2.MedianBlur(matBinary, matBlurred, 3);
matBlurred.CopyTo(matBinary);
LogMessage("✅ Applied median blur noise reduction");
```

**Kết quả:** Loại bỏ nhiễu tốt hơn với median blur

### **2. Multiple PSM Modes**
```csharp
// Try different PSM modes
int[] psmModes = { 6, 7, 8, 13 }; // Single uniform block, Single text line, Single word, Raw line

foreach (int psm in psmModes)
{
    _tessEngine.SetVariable("tessedit_pageseg_mode", psm.ToString());
    // Process and check result
}
```

**Kết quả:** Thử nhiều PSM modes để tìm cái tốt nhất

### **3. Reduced Scale Factor**
```csharp
float scaleFactor = 4.0f; // Scale up 4x for better OCR (reduced from 8x)
```

**Kết quả:** Ảnh 800x320 thay vì 1600x640

## Log mong đợi

Bây giờ sẽ thấy:
```
✅ Applied morphological noise reduction
✅ Applied median blur noise reduction
✅ Upscaled normal image by 4x: 200x80 -> 800x320
🔍 Trying normal image with multiple PSM modes...
🔍 Trying PSM mode 6...
PSM 6: raw='rzjf', confidence=85.50%
✅ PSM 6 success: 'rzjf' (confidence: 85.50%)
✅ OpenCV + Tesseract success: 'rzjf'
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Thử different scale factors:**
   ```csharp
   float scaleFactor = 2.0f; // Scale up 2x
   float scaleFactor = 6.0f; // Scale up 6x
   ```

2. **Thử different PSM modes:**
   ```csharp
   int[] psmModes = { 3, 4, 5 }; // Different modes
   ```

3. **Thử different noise reduction:**
   ```csharp
   Cv2.MedianBlur(matBinary, matBlurred, 5); // Larger kernel
   ```

## Kết quả mong đợi

Với enhanced noise reduction, multiple PSM modes và reduced scale factor, Tesseract sẽ đọc được captcha từ ảnh đã được OpenCV xử lý tối ưu!
