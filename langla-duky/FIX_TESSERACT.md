# 🔧 FIX LỖI TESSERACT ENGINE

## ❌ **Lỗi hiện tại:**
```
Lỗi: Lỗi xử lý captcha: Tesseract engine chưa được khởi tạo
```

## ✅ **Cách fix:**

### **Bước 1: Cài đặt Tesseract OCR**
1. Tải Tesseract từ: https://github.com/UB-Mannheim/tesseract/wiki
2. Chọn "tesseract-ocr-w64-setup-5.3.x.exe" (bản mới nhất)
3. Cài đặt vào `C:\Program Files\Tesseract-OCR\`

### **Bước 2: Copy tessdata (Tự động)**
```bash
# Chạy script tự động
setup_tessdata.bat
```

### **Bước 3: Copy tessdata (Thủ công)**
```bash
# Copy từ:
C:\Program Files\Tesseract-OCR\tessdata\

# Đến:
D:\tool\langla-duky\tessdata\
```

### **Bước 4: Kiểm tra**
Tool sẽ hiển thị:
```
Tesseract engine đã khởi tạo thành công với tessdata: D:\tool\langla-duky\tessdata
```

## 🚀 **Sau khi fix:**

1. **Chạy tool** bằng `dotnet run`
2. **Click "Debug"** để kiểm tra cửa sổ game
3. **Click "Test OCR"** để test OCR
4. **Click "Bắt đầu"** để auto captcha

## 📁 **Cấu trúc thư mục sau khi fix:**
```
langla-duky/
├── tessdata/           ← Thư mục này cần có
│   ├── eng.traineddata ← File này quan trọng
│   ├── osd.traineddata
│   └── ... (các file khác)
├── langla-duky.exe
├── config.json
└── ...
```

## ⚠️ **Lưu ý:**
- Đảm bảo thư mục `tessdata` có file `eng.traineddata`
- Nếu vẫn lỗi, kiểm tra đường dẫn trong `config.json`
- Tool sẽ hiển thị đường dẫn đầy đủ trong log để debug
