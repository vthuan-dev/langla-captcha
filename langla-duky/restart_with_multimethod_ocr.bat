@echo off
echo ===== Khởi động lại Langla-Duky với Multi-Method OCR Workflow =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
if exist "bin\Debug\net8.0-windows\captcha_workflow" rmdir /s /q "bin\Debug\net8.0-windows\captcha_workflow"
echo.

echo Đang build dự án với Multi-Method OCR Workflow...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với Multi-Method OCR Workflow...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  ✅ Multi-Method OCR Workflow với 5 phương pháp:
echo     1. OCR trực tiếp trên ảnh captcha gốc
echo     2. OCR trên ảnh đã scale 3x (tăng độ phân giải)
echo     3. OCR trên ảnh đã đảo màu (invert colors)
echo     4. OCR trên ảnh đã tăng độ tương phản
echo     5. OCR trên ảnh đã preprocessing nâng cao
echo  ✅ Tọa độ captcha chính xác: X=539, Y=306, W=202, H=97
echo  ✅ Lưu tất cả ảnh debug để phân tích
echo  ✅ Log chi tiết cho từng phương pháp OCR
echo.
echo Workflow sẽ thử từng phương pháp cho đến khi thành công:
echo  - Nếu phương pháp 1 thất bại → thử phương pháp 2
echo  - Nếu phương pháp 2 thất bại → thử phương pháp 3
echo  - Và cứ thế cho đến khi tìm được kết quả hoặc hết phương pháp
echo.
echo Các file sẽ được lưu trong captcha_workflow:
echo  - full_window_[timestamp].jpg - Ảnh toàn bộ cửa sổ
echo  - captcha_crop_[timestamp].jpg - Ảnh captcha đã cắt
echo  - captcha_scaled_[timestamp].jpg - Ảnh scale 3x
echo  - captcha_inverted_[timestamp].jpg - Ảnh đảo màu
echo  - captcha_contrast_[timestamp].jpg - Ảnh tăng độ tương phản
echo  - captcha_processed_[timestamp].jpg - Ảnh preprocessing nâng cao
echo  - ocr_result_[timestamp].txt - Kết quả OCR cuối cùng
echo  - debug_info_[timestamp].txt - Thông tin debug nếu tất cả thất bại
echo.
echo Hướng dẫn sử dụng:
echo  1. Click "Chọn cửa sổ" để chọn cửa sổ game Duke Client
echo  2. Khi captcha xuất hiện, click "📸 Capture & Process"
echo  3. Xem log để theo dõi từng phương pháp OCR
echo  4. Kiểm tra các file ảnh để phân tích chất lượng
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
