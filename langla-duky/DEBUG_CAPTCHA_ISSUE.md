# Debug Captcha Issue

## Vấn đề hiện tại

**Lỗi:** Ảnh captcha được capture chỉ có màu trắng, không có text.

**Nguyên nhân có thể:**
1. **Vị trí capture sai** - Coordinates trong config không đúng
2. **Kích thước ảnh quá nhỏ** - 126x59 có thể quá nhỏ cho Tesseract
3. **Captcha chưa xuất hiện** - Game chưa hiển thị captcha
4. **Window resolution khác** - Game window có resolution khác với expected

## Cách debug

### 1. **Kiểm tra ảnh debug**
Mở file ảnh đã được lưu:
```
D:\tool\langla-duky\langla-duky\bin\Debug\net8.0-windows\captcha_debug\captcha_enhanced_20250920_032421_076.png
```

**Nếu ảnh trắng hoàn toàn:**
- Vị trí capture sai
- Captcha chưa xuất hiện
- Window resolution khác

**Nếu ảnh có captcha nhưng mờ:**
- Kích thước quá nhỏ
- Cần tăng kích thước capture area

### 2. **Thử các giải pháp**

#### **Giải pháp A: Bật AutoDetectCaptchaArea**
```json
{
  "AutoDetectCaptchaArea": true
}
```

#### **Giải pháp B: Sử dụng Manual Capture**
1. Click "Set Captcha Area" button
2. Vẽ rectangle quanh vùng captcha
3. Lưu config

#### **Giải pháp C: Điều chỉnh coordinates**
```json
{
  "CaptchaAreaRelative": {
    "X": 0.36,
    "Y": 0.30,
    "Width": 0.10,
    "Height": 0.08
  }
}
```

#### **Giải pháp D: Sử dụng Absolute Coordinates**
```json
{
  "UseAbsoluteCoordinates": true,
  "CaptchaLeftX": 500,
  "CaptchaTopY": 250,
  "CaptchaRightX": 700,
  "CaptchaBottomY": 300
}
```

### 3. **Kiểm tra log mới**

Sau khi sửa, chạy lại và kiểm tra:
- `🔍 Non-white pixels found: X` - Nếu > 0 thì có content
- `💾 Saved processed image` - Kiểm tra ảnh đã xử lý
- `Tesseract raw result` - Nếu có text thì OCR hoạt động

### 4. **Troubleshooting**

**Nếu vẫn không hoạt động:**
1. Kiểm tra game có đang hiển thị captcha không
2. Thử capture toàn bộ màn hình để tìm vị trí captcha
3. Kiểm tra window title có đúng không
4. Thử với resolution khác

## Kết quả mong đợi

Sau khi sửa, log sẽ hiển thị:
```
🔍 Non-white pixels found: 50+ (sampled every 5px)
Tesseract raw result: 'abc123'
✅ OpenCV + Tesseract success: 'abc123'
```
