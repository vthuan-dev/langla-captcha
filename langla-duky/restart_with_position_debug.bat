@echo off
echo ===== Khởi động lại Langla-Duky với thông tin vị trí debug =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
echo.

echo Đang build dự án với thông tin vị trí debug...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với thông tin vị trí debug...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  - Thông tin vị trí chi tiết khi phát hiện captcha thất bại
echo  - Log vị trí chính xác của vùng quét captcha
echo  - Thông tin kích thước cửa sổ game và vùng captcha
echo  - Debug chi tiết cho từng phương pháp phát hiện:
echo    * Standard OCR với vị trí
echo    * Inverted OCR với vị trí  
echo    * Enhanced contrast OCR với vị trí
echo    * Color pattern detection với vị trí
echo    * Dialog pattern detection với vị trí
echo    * Change detection với thông tin mẫu
echo  - Vùng quét captcha: X=500, Y=280, Rộng=280, Cao=120
echo.
echo Bây giờ khi captcha không được phát hiện, bạn sẽ thấy:
echo  - Vị trí chính xác mà công cụ đang quét
echo  - Kích thước vùng quét và cửa sổ game
echo  - Tỷ lệ pixel trống/thay đổi trong ảnh
echo  - Thông tin debug chi tiết cho từng bước
echo.
echo Nếu bạn gặp vấn đề, hãy kiểm tra các thư mục debug:
echo  - bin\Debug\net8.0-windows\ocr_debug_output
echo  - bin\Debug\net8.0-windows\captcha_detections
echo  - bin\Debug\net8.0-windows\debug_images
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
