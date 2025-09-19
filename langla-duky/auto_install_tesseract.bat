@echo off
echo ====================================
echo   Auto Install Tesseract OCR
echo ====================================
echo.

echo Đang tải Tesseract OCR...
echo.

REM Tạo thư mục temp
if not exist "%TEMP%\tesseract" mkdir "%TEMP%\tesseract"

REM Tải Tesseract (sử dụng PowerShell)
powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://github.com/UB-Mannheim/tesseract/releases/download/v5.3.0.20221214/tesseract-ocr-w64-setup-5.3.0.20221214.exe' -OutFile '%TEMP%\tesseract\tesseract-installer.exe'}"

if %ERRORLEVEL% NEQ 0 (
    echo Lỗi tải Tesseract!
    echo Vui lòng tải thủ công từ: https://github.com/UB-Mannheim/tesseract/wiki
    pause
    exit /b 1
)

echo Tải thành công! Đang cài đặt...
echo.

REM Cài đặt Tesseract (silent install)
"%TEMP%\tesseract\tesseract-installer.exe" /S /D=C:\Program Files\Tesseract-OCR

if %ERRORLEVEL% NEQ 0 (
    echo Lỗi cài đặt Tesseract!
    echo Vui lòng cài đặt thủ công từ: %TEMP%\tesseract\tesseract-installer.exe
    pause
    exit /b 1
)

echo Cài đặt thành công!
echo.

REM Copy tessdata
echo Đang copy tessdata...
set TESSERACT_PATH="C:\Program Files\Tesseract-OCR\tessdata"
set TARGET_PATH="%~dp0tessdata"

if exist %TARGET_PATH% (
    echo Thư mục tessdata đã tồn tại, đang xóa...
    rmdir /s /q %TARGET_PATH%
)

xcopy %TESSERACT_PATH% %TARGET_PATH% /E /I /Y

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Setup hoàn tất!
    echo Tesseract OCR đã được cài đặt và tessdata đã được copy.
    echo.
    echo Bây giờ bạn có thể chạy tool captcha automation.
) else (
    echo.
    echo ✗ Lỗi copy tessdata!
    echo Vui lòng copy thủ công từ %TESSERACT_PATH% đến %TARGET_PATH%
)

REM Dọn dẹp
rmdir /s /q "%TEMP%\tesseract"

echo.
pause
