# Debug Analysis Guide

## Vấn đề hiện tại

**Từ log:** `🔍 Non-white pixels found: 19` - Có một ít content nhưng Tesseract không đọc được.

**Nguyên nhân có thể:**
1. **Kích thước ảnh quá nhỏ** - 126x59 pixels
2. **Ảnh có captcha nhưng quá mờ/nhỏ**
3. **Vị trí capture gần đúng nhưng chưa chính xác**

## Cải tiến đã thêm

### 1. **Upscaling 4x**
```csharp
float scaleFactor = 4.0f; // Scale up 4x for better OCR
Cv2.Resize(matBinary, matUpscaled, new OpenCvSharp.Size(
    (int)(matBinary.Width * scaleFactor), 
    (int)(matBinary.Height * scaleFactor)), 
    0, 0, InterpolationFlags.Cubic);
```

**Kết quả:** 126x59 → 504x236 pixels (Tesseract sẽ đọc tốt hơn)

### 2. **Debug images chi tiết**
Bây giờ sẽ lưu tất cả các bước xử lý:
- `captcha_original_*.png` - Ảnh gốc từ game
- `captcha_grayscale_*.png` - Ảnh xám
- `captcha_binary_*.png` - Ảnh trắng đen
- `captcha_processed_*.png` - Ảnh cuối cùng (đã upscale)

## Cách kiểm tra

### 1. **Kiểm tra ảnh gốc**
Mở `captcha_original_*.png`:
- **Nếu có captcha rõ ràng** → Vấn đề ở xử lý OpenCV
- **Nếu chỉ có background** → Vấn đề ở vị trí capture
- **Nếu có captcha nhưng mờ** → Cần điều chỉnh threshold

### 2. **Kiểm tra ảnh binary**
Mở `captcha_binary_*.png`:
- **Nếu có text đen trên nền trắng** → Upscaling sẽ giúp
- **Nếu chỉ có màu trắng** → Threshold quá cao
- **Nếu chỉ có màu đen** → Threshold quá thấp

### 3. **Kiểm tra ảnh processed**
Mở `captcha_processed_*.png`:
- **Nếu text rõ ràng và lớn** → Tesseract sẽ đọc được
- **Nếu vẫn mờ** → Cần tăng scaleFactor

## Log mong đợi

Sau khi sửa, bạn sẽ thấy:
```
✅ Upscaled image by 4x: 126x59 -> 504x236
💾 Saved original image: captcha_original_*.png
💾 Saved grayscale image: captcha_grayscale_*.png  
💾 Saved binary image: captcha_binary_*.png
💾 Saved processed image: captcha_processed_*.png
Tesseract raw result: 'abc123'
✅ OpenCV + Tesseract success: 'abc123'
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Tăng scaleFactor:**
```csharp
float scaleFactor = 6.0f; // Thử 6x thay vì 4x
```

2. **Thử threshold khác:**
```csharp
Cv2.Threshold(matGray, matBinary, 127, 255, ThresholdTypes.Binary); // Fixed threshold
```

3. **Kiểm tra vị trí capture:**
- Mở ảnh gốc và xem captcha có ở đúng vị trí không
- Nếu không, điều chỉnh `CaptchaAreaRelative` trong config

4. **Thử AutoDetectCaptchaArea:**
```json
{
  "AutoDetectCaptchaArea": true
}
```

## Kết quả mong đợi

Với upscaling 4x, ảnh 126x59 sẽ trở thành 504x236 pixels - đủ lớn để Tesseract đọc được text captcha.
