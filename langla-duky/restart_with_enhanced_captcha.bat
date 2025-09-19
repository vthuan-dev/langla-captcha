@echo off
echo ===== Khởi động lại Langla-Duky với cải tiến phát hiện captcha =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
echo.

echo Đang build dự án với cải tiến phát hiện captcha...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với cải tiến phát hiện captcha...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  - Tối ưu hóa chụp ảnh màn hình (giảm kích thước và dung lượng)
echo  - Cải thiện phát hiện captcha với 4 phương pháp:
echo    1. Phát hiện mẫu dialog captcha
echo    2. Phát hiện thay đổi trong vùng captcha (cải tiến)
echo    3. Phát hiện màu sắc đặc trưng của captcha (mới)
echo    4. Phát hiện text bằng OCR (cải tiến)
echo  - Vùng quét captcha đã được mở rộng: X=500, Y=280, Rộng=280, Cao=120
echo  - Điều kiện xác nhận captcha linh hoạt hơn, chấp nhận 2-8 ký tự
echo  - Phát hiện các mẫu captcha phổ biến
echo.
echo Nếu bạn gặp vấn đề, hãy kiểm tra các thư mục debug:
echo  - bin\Debug\net8.0-windows\ocr_debug_output
echo  - bin\Debug\net8.0-windows\captcha_detections
echo  - bin\Debug\net8.0-windows\debug_images
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
