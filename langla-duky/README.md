# Làng Lá Duke - Captcha Automation Tool

Tool tự động nhập mã captcha cho game PC "Làng Lá Duke".

## Tính năng

- ✅ Tự động phát hiện cửa sổ game
- ✅ Chụp và xử lý hình ảnh captcha
- ✅ OCR đọc mã captcha bằng Tesseract
- ✅ Tự động nhập mã captcha
- ✅ Tự động click nút xác nhận
- ✅ UI đơn giản để điều khiển
- ✅ Logging và thống kê

## Yêu cầu hệ thống

- Windows 10/11
- .NET 8.0 Runtime
- Game "Làng Lá Duke" đang chạy

## Cài đặt

### 1. Cài đặt Tesseract OCR

1. Tải Tesseract từ: https://github.com/UB-Mannheim/tesseract/wiki
2. Cài đặt vào thư mục `C:\Program Files\Tesseract-OCR\`
3. Copy thư mục `tessdata` vào thư mục chứa tool

### 2. Build và chạy tool

```bash
cd langla-duky
dotnet restore
dotnet build
dotnet run
```

## Cách sử dụng

1. **Khởi động game** "Làng Lá Duke"
2. **Chạy tool** bằng cách double-click file exe hoặc `dotnet run`
3. **Click "Bắt đầu"** để tool tự động xử lý captcha
4. **Click "Dừng"** để tạm dừng automation
5. **Click "Test OCR"** để test một lần

## Cấu hình

Tool sẽ tự động tìm cửa sổ game với title "Làng Lá Duke". Nếu cần điều chỉnh vị trí các element, có thể modify trong code:

```csharp
// Trong CaptchaAutomationService constructor
_captchaArea = new Rectangle(x, y, width, height); // Vùng captcha
_inputFieldPosition = new Point(x, y); // Vị trí ô input
_confirmButtonPosition = new Point(x, y); // Vị trí nút xác nhận
```

## Troubleshooting

### Lỗi "Không tìm thấy cửa sổ game"
- Đảm bảo game đang chạy
- Kiểm tra title cửa sổ game có đúng "Làng Lá Duke"

### Lỗi OCR không đọc được captcha
- Kiểm tra Tesseract đã cài đặt đúng
- Thử điều chỉnh vùng captcha trong code
- Kiểm tra chất lượng hình ảnh captcha

### Tool không nhập được text
- Đảm bảo game đang active
- Kiểm tra vị trí ô input có đúng không
- Thử click vào ô input trước khi chạy tool

## Cấu trúc project

```
langla-duky/
├── Models/
│   ├── GameWindow.cs          # Quản lý cửa sổ game
│   ├── ScreenCapture.cs       # Chụp màn hình và xử lý hình ảnh
│   ├── CaptchaReader.cs      # OCR đọc captcha
│   └── InputAutomation.cs    # Tự động nhập và click
├── Services/
│   └── CaptchaAutomationService.cs # Service chính
├── MainForm.cs               # UI Windows Forms
├── Program.cs                # Entry point
└── langla-duky.csproj       # Project file
```

## Lưu ý

- Tool chỉ hoạt động với game "Làng Lá Duke"
- Cần điều chỉnh vị trí các element dựa trên resolution màn hình
- OCR có thể không chính xác 100%, cần test và fine-tune
- Sử dụng tool có trách nhiệm và tuân thủ quy định game

## License

MIT License - Sử dụng tự do cho mục đích học tập và nghiên cứu.
