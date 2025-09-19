# Hướng dẫn chuyển từ IronOCR sang Tesseract OCR

Tài liệu này hướng dẫn cách chuyển đổi từ thư viện IronOCR sang Tesseract OCR trực tiếp trong dự án "Làng Lá Duke - Captcha Automation Tool".

## Lý do chuyển đổi

- **Hiệu suất tốt hơn**: Tesseract OCR trực tiếp cho kết quả nhận dạng tốt hơn với captcha của game
- **Kiểm soát tốt hơn**: Có thể tùy chỉnh nhiều thông số của Tesseract
- **Mã nguồn mở**: Tesseract là thư viện mã nguồn mở, không phụ thuộc vào license
- **Cộng đồng lớn**: Có nhiều tài liệu và hỗ trợ từ cộng đồng

## Các thay đổi chính

1. **Thay đổi package reference**:
   - Gỡ bỏ `IronOcr`
   - Thêm `Tesseract` (phiên bản 5.2.0)

2. **Tạo class mới**:
   - Tạo `TesseractCaptchaReader` để thay thế `CaptchaReader`
   - Thêm các phương thức xử lý hình ảnh tương tự

3. **Cập nhật ScreenCapture**:
   - Chuyển các phương thức xử lý hình ảnh từ private sang public
   - Thêm helper class `PixConverter` để chuyển đổi giữa Bitmap và Pix

4. **Cập nhật CaptchaAutomationService**:
   - Thay đổi kiểu của `_captchaReader` từ `CaptchaReader` sang `TesseractCaptchaReader`

## Cách sử dụng

### 1. Cài đặt Tesseract data files

Chạy script `update_tessdata.bat` để tải và cài đặt các file traineddata cần thiết:

```
update_tessdata.bat
```

Script này sẽ:
- Tạo thư mục `tessdata` nếu chưa tồn tại
- Tải `eng.traineddata` và `osd.traineddata` nếu chưa có
- Copy các file vào thư mục bin

### 2. Xây dựng và chạy ứng dụng

```
dotnet build
dotnet run
```

## Cấu trúc mới của TesseractCaptchaReader

```csharp
public class TesseractCaptchaReader : IDisposable
{
    private TesseractEngine? _engine;
    
    // Khởi tạo Tesseract Engine
    public TesseractCaptchaReader(string tessDataPath = @"./tessdata", string language = "eng")
    {
        // ...
    }
    
    // Đọc captcha với nhiều phương pháp khác nhau
    public string ReadCaptcha(Bitmap captchaImage)
    {
        // ...
    }
    
    // Các phương thức tiền xử lý hình ảnh
    private Pix PreprocessImage(Bitmap image)
    {
        // ...
    }
    
    // Kiểm tra captcha có hợp lệ không
    public bool IsValidCaptcha(string text)
    {
        // ...
    }
    
    // Giải phóng tài nguyên
    public void Dispose()
    {
        // ...
    }
}
```

## Tùy chỉnh thêm

Bạn có thể tùy chỉnh các thông số của Tesseract để cải thiện kết quả OCR:

```csharp
// Trong TesseractCaptchaReader.cs
_engine.SetVariable("tessedit_char_whitelist", "abcdefghjkmnpqrstuvwxyABCDEFGHJKLMNPQRSTVWXYZ123456789");
_engine.SetVariable("tessedit_char_blacklist", "!@#$%^&*()_+-={}[]|\\:;\"'<>?,./iIlLoO0");
_engine.SetVariable("classify_bln_numeric_mode", "0");
_engine.SetVariable("tessedit_pageseg_mode", "6"); // Assume a single uniform block of text
```

## Troubleshooting

### Lỗi không tìm thấy tessdata

Nếu gặp lỗi "Failed to load language 'eng'", hãy kiểm tra:
- Thư mục `tessdata` đã có trong thư mục bin chưa
- File `eng.traineddata` đã tồn tại trong thư mục `tessdata` chưa
- Đường dẫn đến `tessdata` trong config có chính xác không

### Lỗi nhận dạng kém

Nếu kết quả OCR không tốt:
- Thử điều chỉnh phương pháp tiền xử lý hình ảnh
- Thay đổi các thông số của Tesseract
- Thử sử dụng các traineddata khác (như `eng.traineddata` phiên bản cũ hơn)

## Tài liệu tham khảo

- [Tesseract OCR GitHub](https://github.com/tesseract-ocr/tesseract)
- [Tesseract .NET Wrapper](https://github.com/charlesw/tesseract)
- [Tesseract Documentation](https://tesseract-ocr.github.io/tessdoc/)
