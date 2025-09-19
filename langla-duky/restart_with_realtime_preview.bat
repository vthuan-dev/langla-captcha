@echo off
echo ===== Khởi động lại Langla-Duky với Preview Real-time và Cải tiến Captcha =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
echo.

echo Đang build dự án với preview real-time và cải tiến captcha...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với preview real-time và cải tiến captcha...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  ✅ Preview real-time cửa sổ game (cập nhật mỗi 0.5 giây)
echo  ✅ Hiển thị ảnh ngay khi chọn cửa sổ game
echo  ✅ Cải thiện phát hiện captcha có khoảng trắng (như "e u m f")
echo  ✅ Vùng quét captcha mở rộng: X=450, Y=250, Rộng=350, Cao=150
echo  ✅ Thông tin vị trí chi tiết khi phát hiện thất bại
echo  ✅ Tối ưu hóa lưu ảnh (JPEG thay vì PNG)
echo.
echo Hướng dẫn sử dụng:
echo  1. Click "Chọn cửa sổ" để chọn cửa sổ game
echo  2. Preview real-time sẽ hiển thị ở khung bên phải
echo  3. Click "Monitor" để bắt đầu phát hiện captcha
echo  4. Khi captcha xuất hiện, công cụ sẽ tự động đọc và nhập
echo.
echo Nếu bạn gặp vấn đề, hãy kiểm tra các thư mục debug:
echo  - bin\Debug\net8.0-windows\ocr_debug_output
echo  - bin\Debug\net8.0-windows\captcha_detections
echo  - bin\Debug\net8.0-windows\debug_images
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
