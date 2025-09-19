# Hướng dẫn Setup Tool Làng Lá Duke

## 1. Cài đặt Prerequisites

### .NET 8.0 Runtime
1. Tải từ: https://dotnet.microsoft.com/download/dotnet/8.0
2. Chọn "Download .NET Desktop Runtime 8.0.x (Windows x64)"
3. Cài đặt và restart máy

### Tesseract OCR
1. **Tải Tesseract:**
   - Link: https://github.com/UB-Mannheim/tesseract/wiki
   - Chọn "tesseract-ocr-w64-setup-5.3.x.exe" (bản mới nhất)

2. **Cài đặt Tesseract:**
   - Chạy file installer
   - **QUAN TRỌNG**: Chọn đường dẫn cài đặt: `C:\Program Files\Tesseract-OCR\`
   - Tick chọn "Add to PATH" nếu có

3. **Copy tessdata folder:**
   ```
   Copy thư mục: C:\Program Files\Tesseract-OCR\tessdata\
   Paste vào: <thư_mục_tool>\tessdata\
   ```

## 2. Build Tool

### Cách 1: Sử dụng batch file
```bash
# Double-click file build.bat
build.bat
```

### Cách 2: Sử dụng command line
```bash
cd langla-duky
dotnet restore
dotnet build --configuration Release
```

## 3. Cấu hình Tool

### File config.json
Tool sử dụng file `config.json` để cấu hình vị trí các element:

```json
{
  "GameWindowTitle": "Làng Lá Duke",
  "CaptchaArea": {
    "X": 300,     // Vị trí X của vùng captcha
    "Y": 250,     // Vị trí Y của vùng captcha  
    "Width": 200, // Chiều rộng vùng captcha
    "Height": 80  // Chiều cao vùng captcha
  },
  "InputFieldPosition": {
    "X": 400,     // Vị trí X của ô input
    "Y": 350      // Vị trí Y của ô input
  },
  "ConfirmButtonPosition": {
    "X": 490,     // Vị trí X của nút xác nhận
    "Y": 380      // Vị trí Y của nút xác nhận
  }
}
```

### Cách tìm tọa độ chính xác:

1. **Mở game** và đến màn hình captcha
2. **Chụp screenshot** màn hình
3. **Mở Paint** và paste screenshot
4. **Di chuột** để xem tọa độ ở góc dưới trái
5. **Ghi lại tọa độ** của:
   - Góc trái trên của vùng captcha (X, Y)
   - Kích thước vùng captcha (Width, Height)  
   - Vị trí ô input (X, Y)
   - Vị trí nút xác nhận (X, Y)

## 4. Chạy Tool

### Cách 1: Sử dụng batch file
```bash
# Double-click file run.bat
run.bat
```

### Cách 2: Chạy từ source
```bash
cd langla-duky
dotnet run
```

### Cách 3: Chạy file exe
```bash
cd langla-duky/bin/Release/net8.0-windows/
langla-duky.exe
```

## 5. Sử dụng Tool

1. **Khởi động game** "Làng Lá Duke"
2. **Chạy tool** 
3. **Click "Bắt đầu"** để auto captcha
4. **Click "Test OCR"** để test một lần
5. **Click "Dừng"** để tạm dừng

## 6. Troubleshooting

### Lỗi "Không tìm thấy cửa sổ game"
- Đảm bảo game đang chạy
- Kiểm tra title cửa sổ có đúng "Làng Lá Duke"
- Thử đổi `GameWindowTitle` trong config.json

### Lỗi "Tesseract engine chưa được khởi tạo"
- Kiểm tra Tesseract đã cài đặt đúng
- Kiểm tra thư mục `tessdata` có tồn tại
- Thử đổi đường dẫn `TessdataPath` trong config

### OCR đọc sai captcha
- Điều chỉnh vùng `CaptchaArea` cho chính xác
- Kiểm tra chất lượng hình ảnh captcha
- Thử chỉnh sửa `CharWhitelist` trong config

### Tool không nhập được text
- Đảm bảo game đang active
- Kiểm tra vị trí `InputFieldPosition`
- Thử click vào ô input trước khi chạy tool

### Tool không click được nút
- Kiểm tra vị trí `ConfirmButtonPosition`
- Đảm bảo nút có thể click được
- Thử điều chỉnh delay trong config

## 7. Tips Tối ưu

1. **Chạy game ở windowed mode** để tool hoạt động tốt hơn
2. **Đặt game ở vị trí cố định** trên màn hình
3. **Không di chuyển cửa sổ game** khi tool đang chạy
4. **Test từng bước** bằng nút "Test OCR" trước khi auto
5. **Backup config.json** sau khi setup xong

## 8. Cấu trúc Files

```
langla-duky/
├── bin/Release/net8.0-windows/    # File exe sau khi build
├── tessdata/                     # Tesseract language data
├── config.json                   # File cấu hình
├── run.bat                      # Script chạy tool
├── build.bat                    # Script build tool
├── README.md                    # Hướng dẫn tổng quan
└── SETUP.md                     # Hướng dẫn setup chi tiết
```
