# Captcha Area Debug Guide

## Vấn đề hiện tại

**Từ log:** `🔍 Non-white pixels found: 19` và `Tesseract raw result: ''`

**Nguyên nhân:** Vị trí capture không đúng - ảnh chủ yếu là màu trắng

## Cải tiến đã thêm

### 1. **AutoDetectCaptchaArea = true**
```json
{
  "AutoDetectCaptchaArea": true
}
```

**Kết quả:** Tự động tìm captcha thay vì dùng tọa độ cố định

### 2. **Debug ảnh gốc chi tiết**
Bây giờ sẽ kiểm tra ảnh gốc có captcha không:
- `🔍 Original image analysis: X non-white pixels`
- Cảnh báo nếu ảnh chủ yếu là trắng

## Cách kiểm tra

### 1. **Kiểm tra ảnh gốc**
Mở `captcha_original_*.png`:
- **Nếu có captcha rõ ràng** → Vấn đề ở xử lý OpenCV
- **Nếu chỉ có background trắng** → Vấn đề ở vị trí capture
- **Nếu có captcha nhưng mờ** → Cần điều chỉnh threshold

### 2. **Kiểm tra log mới**
Bây giờ sẽ thấy:
```
🔍 Original image analysis: 150 non-white pixels (sampled every 5px)
```
- **Nếu < 5 pixels** → Vị trí capture sai
- **Nếu > 50 pixels** → Có captcha, vấn đề ở xử lý

### 3. **Kiểm tra AutoDetect**
Với `AutoDetectCaptchaArea: true`, log sẽ thấy:
```
ROI method: auto-detect (client) area={X=XXX,Y=YYY,Width=WWW,Height=HHH}
```

## Troubleshooting

### Nếu vẫn không hoạt động:

1. **Kiểm tra game window:**
   - Captcha có hiển thị trong game không?
   - Captcha có ở vị trí khác không?

2. **Thử manual capture:**
   ```json
   {
     "UseManualCapture": true
   }
   ```

3. **Điều chỉnh tọa độ thủ công:**
   ```json
   {
     "CaptchaArea": {
       "X": 100,
       "Y": 200,
       "Width": 300,
       "Height": 100
     }
   }
   ```

4. **Thử absolute coordinates:**
   ```json
   {
     "UseAbsoluteCoordinates": true,
     "UseRelativeCoordinates": false
   }
   ```

## Log mong đợi

Sau khi sửa, bạn sẽ thấy:
```
ROI method: auto-detect (client) area={X=XXX,Y=YYY,Width=WWW,Height=HHH}
🔍 Original image analysis: 150 non-white pixels (sampled every 5px)
✅ Upscaled image by 4x: 126x59 -> 504x236
Tesseract raw result: 'abc123'
✅ OpenCV + Tesseract success: 'abc123'
```

## Kết quả mong đợi

Với `AutoDetectCaptchaArea: true`, hệ thống sẽ tự động tìm captcha và capture đúng vị trí thay vì dùng tọa độ cố định có thể sai.
