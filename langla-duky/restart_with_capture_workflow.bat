@echo off
echo ===== Khởi động lại Langla-Duky với Capture & Process Workflow =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
if exist "bin\Debug\net8.0-windows\captcha_workflow" rmdir /s /q "bin\Debug\net8.0-windows\captcha_workflow"
echo.

echo Đang build dự án với Capture & Process Workflow...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với Capture & Process Workflow...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  ✅ Nút "📸 Capture & Process" mới
echo  ✅ Workflow xử lý captcha theo 3 bước:
echo     1. Capture toàn bộ cửa sổ game
echo     2. Cắt vùng captcha theo tọa độ chính xác (X=539, Y=306, W=202, H=97)
echo     3. Xử lý OCR và lưu kết quả
echo  ✅ Tọa độ captcha đã được cập nhật theo hình ảnh bạn gửi
echo  ✅ Preview real-time cửa sổ game
echo  ✅ Lưu tất cả ảnh và kết quả vào folder "captcha_workflow"
echo.
echo Hướng dẫn sử dụng:
echo  1. Click "Chọn cửa sổ" để chọn cửa sổ game Duke Client
echo  2. Khi captcha xuất hiện, click "📸 Capture & Process"
echo  3. Ứng dụng sẽ:
echo     - Capture toàn bộ cửa sổ game
echo     - Cắt vùng captcha theo tọa độ chính xác
echo     - Xử lý OCR và hiển thị kết quả
echo     - Lưu tất cả vào folder "captcha_workflow"
echo.
echo Các file sẽ được lưu:
echo  - full_window_[timestamp].jpg - Ảnh toàn bộ cửa sổ
echo  - captcha_crop_[timestamp].jpg - Ảnh captcha đã cắt
echo  - ocr_result_[timestamp].txt - Kết quả OCR
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
