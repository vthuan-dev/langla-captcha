# 🔍 OCR Debugger - Hướng dẫn sử dụng

## 📋 Mục đích
Tool này giúp bạn debug riêng phần OCR trước khi tích hợp vào game, giúp:
- Test OCR với hình ảnh captcha thật
- So sánh các phương pháp xử lý hình ảnh khác nhau
- Tìm ra phương pháp OCR tốt nhất
- Debug các vấn đề về độ chính xác

## 🚀 Cách chạy

### Cách 1: Sử dụng batch file (Khuyến nghị)
```bash
# Double-click file này
run_ocr_debug.bat
```

### Cách 2: Sử dụng command line
```bash
cd langla-duky
dotnet run --configuration Debug
```

## 📁 Cấu trúc thư mục

```
langla-duky/
├── test_images/              # Thư mục chứa hình ảnh test
├── ocr_debug_output/         # Thư mục chứa hình ảnh đã xử lý
├── tessdata/                 # Thư mục chứa Tesseract data
├── OCRDebugger.cs           # Class chính của OCR debugger
├── OCRDebugConsole.cs       # Console application
└── run_ocr_debug.bat        # Batch file để chạy
```

## 🎯 Các tính năng chính

### 1. Test tất cả hình ảnh
- Test OCR với tất cả hình ảnh trong thư mục `test_images`
- Hiển thị kết quả chi tiết cho từng hình ảnh
- Thống kê tỷ lệ thành công

### 2. Test hình ảnh cụ thể
- Test OCR với một hình ảnh cụ thể
- Hiển thị kết quả từ tất cả phương pháp xử lý

### 3. Tạo hình ảnh test mẫu
- Tạo các hình ảnh captcha mẫu để test
- Sử dụng khi chưa có hình ảnh captcha thật

### 4. Hướng dẫn sử dụng
- Hiển thị hướng dẫn chi tiết

### 5. Thông tin debug
- Kiểm tra cài đặt Tesseract
- Kiểm tra thư mục và file cần thiết

## 🔧 Các phương pháp xử lý hình ảnh

Tool sẽ test với các phương pháp sau:

1. **Enhanced Preprocessing**: Xử lý đầy đủ (pink isolation + grayscale + threshold + scale + morphological)
2. **Pink/Magenta Isolation**: Chỉ isolate màu hồng/magenta
3. **Grayscale + Threshold**: Chuyển grayscale và áp dụng threshold
4. **Scale 2x/3x/4x**: Scale hình ảnh với các tỷ lệ khác nhau
5. **Adaptive Threshold**: Threshold thích ứng

## 📊 Cách đọc kết quả

### Kết quả thành công ✅
```
✅ SUCCESS: 'A1B2'
```
- OCR đọc được text và text hợp lệ (4 ký tự)

### Kết quả thất bại ❌
```
❌ FAILED: Không đọc được text
```
- OCR không đọc được text hoặc text không hợp lệ

### Tất cả kết quả
```
📋 Tất cả kết quả: ['A1B2', 'A1B2', '', 'A1B2', 'A1B2', 'A1B2', 'A1B2']
```
- Hiển thị kết quả từ tất cả phương pháp

## 🛠️ Debug workflow

### Bước 1: Chuẩn bị hình ảnh
1. Chụp hình ảnh captcha từ game
2. Copy vào thư mục `test_images`
3. Hoặc sử dụng tùy chọn "Tạo hình ảnh test mẫu"

### Bước 2: Chạy test
1. Chạy OCR debugger
2. Chọn "Test tất cả hình ảnh"
3. Xem kết quả trong console

### Bước 3: Phân tích kết quả
1. Kiểm tra hình ảnh đã xử lý trong `ocr_debug_output`
2. So sánh kết quả từ các phương pháp khác nhau
3. Xác định phương pháp tốt nhất

### Bước 4: Điều chỉnh
1. Nếu kết quả không tốt, điều chỉnh tham số trong code
2. Test lại với hình ảnh mới
3. Lặp lại cho đến khi đạt kết quả mong muốn

## 🔍 Các vấn đề thường gặp

### 1. Tesseract không khởi tạo được
```
❌ Lỗi khởi tạo Tesseract: ...
```
**Giải pháp:**
- Kiểm tra thư mục `tessdata` có tồn tại
- Kiểm tra file `eng.traineddata` có trong `tessdata`
- Cài đặt lại Tesseract OCR

### 2. Không đọc được text
```
❌ FAILED: Không đọc được text
```
**Giải pháp:**
- Kiểm tra chất lượng hình ảnh captcha
- Thử các phương pháp xử lý khác nhau
- Điều chỉnh vùng capture captcha

### 3. Text không hợp lệ
```
❌ FAILED: Captcha không hợp lệ: 'ABC'
```
**Giải pháp:**
- Kiểm tra captcha có đúng 4 ký tự không
- Điều chỉnh logic validation trong `IsValidCaptcha`

## 💡 Tips tối ưu

1. **Sử dụng hình ảnh chất lượng cao**: Captcha rõ nét, không bị mờ
2. **Test với nhiều hình ảnh**: Đảm bảo OCR hoạt động ổn định
3. **So sánh các phương pháp**: Tìm ra phương pháp tốt nhất cho loại captcha của bạn
4. **Lưu hình ảnh debug**: Kiểm tra hình ảnh đã xử lý để hiểu vấn đề
5. **Điều chỉnh tham số**: Thay đổi threshold, scale factor, etc.

## 🎯 Kết quả mong đợi

Sau khi debug thành công, bạn sẽ có:
- ✅ OCR đọc được captcha với độ chính xác cao (>80%)
- ✅ Xác định được phương pháp xử lý hình ảnh tốt nhất
- ✅ Các tham số tối ưu cho loại captcha của game
- ✅ Sẵn sàng tích hợp vào game chính

## 📞 Hỗ trợ

Nếu gặp vấn đề:
1. Kiểm tra log trong console
2. Xem hình ảnh debug trong `ocr_debug_output`
3. Kiểm tra cài đặt Tesseract
4. Thử với hình ảnh test mẫu trước
