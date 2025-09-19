@echo off
echo ===== Khởi động lại Langla-Duky với Advanced Color Processing =====
echo.
echo Đang dọn dẹp các lần chạy trước...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
if exist "bin\Debug\net8.0-windows\captcha_workflow" rmdir /s /q "bin\Debug\net8.0-windows\captcha_workflow"
echo.

echo Đang build dự án với Advanced Color Processing...
dotnet build
echo.

echo Đang sao chép các file tessdata vào thư mục output...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Khởi động ứng dụng với Advanced Color Processing...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Hoàn tất! Ứng dụng đã được khởi động với:
echo  ✅ Image Quality Analysis - Phân tích chất lượng ảnh captcha
echo  ✅ 8 phương pháp OCR khác nhau:
echo     1. OCR trực tiếp trên ảnh captcha gốc
echo     2. OCR trên ảnh đã scale 3x
echo     3. OCR trên ảnh đã đảo màu
echo     4. OCR trên ảnh đã tăng độ tương phản
echo     5. OCR trên ảnh đã preprocessing nâng cao
echo     6. OCR trên ảnh đã scale cực lớn (5x)
echo     7. OCR với xử lý màu sắc chuyên biệt ⭐ MỚI
echo     8. OCR với grayscale và threshold tự động ⭐ MỚI
echo  ✅ Advanced Color Processing:
echo     - Phân tích màu sắc để tìm background
echo     - Tạo độ tương phản cao dựa trên màu chủ đạo
echo     - Chuyển đổi grayscale với Otsu threshold
echo     - Xử lý màu sắc phức tạp (98% màu)
echo  ✅ Tọa độ captcha chính xác: X=539, Y=306, W=202, H=97
echo.
echo Workflow sẽ:
echo  1. Capture toàn bộ cửa sổ game
echo  2. Cắt vùng captcha theo tọa độ chính xác
echo  3. Phân tích chất lượng ảnh captcha
echo  4. Thử 8 phương pháp OCR khác nhau
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
echo  - captcha_color_processed_[timestamp].jpg - Ảnh xử lý màu sắc ⭐ MỚI
echo  - captcha_grayscale_[timestamp].jpg - Ảnh grayscale threshold ⭐ MỚI
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
echo  6. Đặc biệt chú ý file captcha_color_processed và captcha_grayscale
echo.
echo Phương pháp mới sẽ xử lý:
echo  - Captcha có 98% màu sắc phức tạp
echo  - Độ tương phản kém
echo  - Màu sắc đa dạng nhưng không rõ ràng
echo.
echo Nhấn phím bất kỳ để thoát...
pause > nul
