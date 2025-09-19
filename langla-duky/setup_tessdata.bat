@echo off
echo ====================================
echo   Setup Tesseract Tessdata
echo ====================================
echo.

set TESSERACT_PATH="C:\Program Files\Tesseract-OCR\tessdata"
set TARGET_PATH="%~dp0tessdata"

echo Đang kiểm tra Tesseract...
if not exist %TESSERACT_PATH% (
    echo Lỗi: Không tìm thấy Tesseract tại %TESSERACT_PATH%
    echo.
    echo Vui lòng cài đặt Tesseract OCR trước:
    echo 1. Tải từ: https://github.com/UB-Mannheim/tesseract/wiki
    echo 2. Cài đặt vào C:\Program Files\Tesseract-OCR\
    echo.
    pause
    exit /b 1
)

echo Tìm thấy Tesseract tại: %TESSERACT_PATH%
echo.

echo Đang copy tessdata...
if exist %TARGET_PATH% (
    echo Thư mục tessdata đã tồn tại, đang xóa...
    rmdir /s /q %TARGET_PATH%
)

xcopy %TESSERACT_PATH% %TARGET_PATH% /E /I /Y

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Copy tessdata thành công!
    echo Thư mục tessdata đã được tạo tại: %TARGET_PATH%
    echo.
    echo Bây giờ bạn có thể chạy tool captcha automation.
) else (
    echo.
    echo ✗ Lỗi copy tessdata!
    echo Vui lòng copy thủ công từ %TESSERACT_PATH% đến %TARGET_PATH%
)

echo.
pause
