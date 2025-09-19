@echo off
echo 🔍 QUICK OCR TEST
echo =================
echo Test OCR với hình ảnh ocr-1.png
echo.

REM Kiểm tra xem đã build chưa
if not exist "bin\Debug\net8.0-windows\langla-duky.exe" (
    echo 📦 Building project...
    dotnet build --configuration Debug
    if errorlevel 1 (
        echo ❌ Build failed!
        pause
        exit /b 1
    )
)

REM Tạo thư mục debug output nếu chưa có
if not exist "ocr_debug_output" (
    mkdir "ocr_debug_output"
    echo 📁 Created ocr_debug_output folder
)

echo.
echo 🚀 Starting Quick OCR Test...
echo.

REM Chạy Quick OCR Test
dotnet run --project . --configuration Debug

pause
