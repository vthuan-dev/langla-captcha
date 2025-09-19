# Hướng dẫn sử dụng tool với Duke Client

## Giới thiệu

Tool này đã được cập nhật để xử lý đặc biệt captcha của Duke Client, với các tính năng:

- Xử lý captcha có khoảng trắng (như "o m b l")
- Nhận diện màu đỏ/cam đặc trưng của Duke Client
- Tự động nhập và click xác nhận

## Cài đặt

1. Đảm bảo đã cài đặt Tesseract OCR và có thư mục `tessdata` với file `eng.traineddata`
2. Chạy file `restart_with_duke_client.bat` để khởi động tool với cấu hình Duke Client

## Cách sử dụng

1. Khởi động Duke Client
2. Khởi động tool bằng file `restart_with_duke_client.bat`
3. Trong tool, click nút "Chọn cửa sổ" và chọn cửa sổ Duke Client
4. Khi xuất hiện captcha, click nút "🎮 Duke Captcha"
5. Tool sẽ tự động:
   - Chụp ảnh captcha
   - Xử lý và nhận dạng text
   - Nhập captcha vào ô input
   - Click nút xác nhận

## Xử lý sự cố

### Captcha không được nhận dạng

- Kiểm tra vị trí captcha trong file `config.json`
- Điều chỉnh các giá trị `CaptchaLeftX`, `CaptchaTopY`, `CaptchaRightX`, `CaptchaBottomY`
- Xem các ảnh debug trong thư mục `duke_captcha_debug` để điều chỉnh

### Không click được nút xác nhận

- Điều chỉnh vị trí nút trong file `config.json`
- Thay đổi giá trị `ConfirmButtonX` và `ConfirmButtonY`

### Lỗi heap corruption (0xc0000374)

Nếu vẫn gặp lỗi heap corruption:

1. Chạy `quick_fix_tessdata.bat` để sửa lỗi tessdata
2. Khởi động lại máy tính
3. Đảm bảo đã cài đặt .NET 8.0 Runtime
4. Thử sử dụng nút "🔬 Test Direct" thay vì "🎮 Duke Captcha"

## Cấu hình nâng cao

Bạn có thể điều chỉnh thêm các tham số trong file `config.json`:

```json
{
  "GameWindowTitle": "Duke Client - By iamDuke",
  "OCRSettings": {
    "CharWhitelist": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 "
  },
  "AutomationSettings": {
    "DelayBetweenAttempts": 2000,
    "DelayAfterInput": 500,
    "DelayAfterClick": 300
  }
}
```

## Liên hệ hỗ trợ

Nếu gặp vấn đề, vui lòng tạo issue trên GitHub hoặc liên hệ qua email.
