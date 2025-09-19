@echo off
echo ===== Khởi động lại Langla-Duky với Image Quality Analysis =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
if exist "bin\Debug\net8.0-windows\captcha_workflow" rmdir /s /q "bin\Debug\net8.0-windows\captcha_workflow"
echo.

echo Đang build dự án với Image Quality Analysis...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với Image Quality Analysis...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  ✅ Image Quality Analysis - Phân tích chất lượng ảnh captcha
echo  ✅ 6 phương pháp OCR khác nhau:
echo     1. OCR trực tiếp trên ảnh captcha gốc
echo     2. OCR trên ảnh đã scale 3x
echo     3. OCR trên ảnh đã đảo màu
echo     4. OCR trên ảnh đã tăng độ tương phản
echo     5. OCR trên ảnh đã preprocessing nâng cao
echo     6. OCR trên ảnh đã scale cực lớn (5x)
echo  ✅ Phân tích chất lượng ảnh:
echo     - Phân tích màu sắc (trắng, đen, xám, màu)
echo     - Độ sáng trung bình
echo     - Đánh giá độ tương phản
echo     - Kiểm tra kích thước hợp lý
echo     - Đưa ra khuyến nghị
echo  ✅ Tọa độ captcha chính xác: X=539, Y=306, W=202, H=97
echo.
echo Workflow sẽ:
echo  1. Capture toàn bộ cửa sổ game
echo  2. Cắt vùng captcha theo tọa độ chính xác
echo  3. Phân tích chất lượng ảnh captcha
echo  4. Thử 6 phương pháp OCR khác nhau
echo  5. Lưu tất cả ảnh và báo cáo phân tích
echo.
echo Các file sẽ được lưu trong captcha_workflow:
echo  - full_window_[timestamp].jpg - Ảnh toàn bộ cửa sổ
echo  - captcha_crop_[timestamp].jpg - Ảnh captcha đã cắt
echo  - captcha_scaled_[timestamp].jpg - Ảnh scale 3x
echo  - captcha_inverted_[timestamp].jpg - Ảnh đảo màu
echo  - captcha_contrast_[timestamp].jpg - Ảnh tăng độ tương phản
echo  - captcha_processed_[timestamp].jpg - Ảnh preprocessing nâng cao
echo  - captcha_mega_scaled_[timestamp].jpg - Ảnh scale 5x
echo  - analysis_[timestamp].txt - Báo cáo phân tích chất lượng
echo  - ocr_result_[timestamp].txt - Kết quả OCR cuối cùng
echo  - debug_info_[timestamp].txt - Thông tin debug nếu tất cả thất bại
echo.
echo Hướng dẫn sử dụng:
echo  1. Click "Chọn cửa sổ" để chọn cửa sổ game Duke Client
echo  2. Khi captcha xuất hiện, click "📸 Capture & Process"
echo  3. Xem log để theo dõi phân tích chất lượng và từng phương pháp OCR
echo  4. Kiểm tra file analysis_[timestamp].txt để xem đánh giá chất lượng
echo  5. Kiểm tra các file ảnh để phân tích trực quan
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
