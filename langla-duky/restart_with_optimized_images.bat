@echo off
echo ===== Khởi động lại Langla-Duky với tối ưu hóa hình ảnh =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
echo.

echo Đang build dự án với tối ưu hóa hình ảnh...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với tối ưu hóa hình ảnh...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  - Tối ưu hóa chụp ảnh màn hình (giảm kích thước và dung lượng)
echo  - Lưu ảnh với định dạng JPEG thay vì PNG (giảm 70-80% dung lượng)
echo  - Cấu hình captcha đã được tối ưu theo khoảng cách đo lường chính xác
echo  - Vùng captcha: X=539, Y=306, Rộng=202, Cao=72
echo  - Vị trí ô nhập liệu: X=640, Y=470
echo  - Vị trí nút xác nhận: X=640, Y=600
echo.
echo Nếu bạn gặp vấn đề, hãy kiểm tra các thư mục debug:
echo  - bin\Debug\net8.0-windows\ocr_debug_output
echo  - bin\Debug\net8.0-windows\captcha_detections
echo  - bin\Debug\net8.0-windows\debug_images
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
