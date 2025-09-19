@echo off
echo ====================================
echo   Quick Fix Tessdata
echo ====================================
echo.

REM Thử các vị trí có thể có Tesseract
set "PATHS[0]=C:\Program Files\Tesseract-OCR\tessdata"
set "PATHS[1]=C:\Program Files (x86)\Tesseract-OCR\tessdata"
set "PATHS[2]=C:\tesseract\tessdata"
set "PATHS[3]=%USERPROFILE%\AppData\Local\Tesseract-OCR\tessdata"

echo Đang tìm Tesseract tessdata...

for /L %%i in (0,1,3) do (
    call set "CURRENT_PATH=%%PATHS[%%i]%%"
    if exist "!CURRENT_PATH!" (
        echo Tìm thấy tessdata tại: !CURRENT_PATH!
        goto :found
    )
)

echo Không tìm thấy tessdata!
echo.
echo Vui lòng:
echo 1. Cài đặt Tesseract OCR từ: https://github.com/UB-Mannheim/tesseract/wiki
echo 2. Hoặc chạy: auto_install_tesseract.bat
echo.
pause
exit /b 1

:found
set TARGET_PATH="%~dp0tessdata"

echo Đang copy tessdata...
if exist %TARGET_PATH% (
    echo Thư mục tessdata đã tồn tại, đang xóa...
    rmdir /s /q %TARGET_PATH%
)

xcopy "!CURRENT_PATH!" %TARGET_PATH% /E /I /Y

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Copy tessdata thành công!
    echo Thư mục tessdata đã được tạo tại: %TARGET_PATH%
    echo.
    echo Bây giờ bạn có thể chạy tool captcha automation.
) else (
    echo.
    echo ✗ Lỗi copy tessdata!
    echo Vui lòng copy thủ công từ !CURRENT_PATH! đến %TARGET_PATH%
)

echo.
pause
