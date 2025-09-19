@echo off
echo 🔍 OCR DEBUG CONSOLE
echo ===================
echo Tool debug OCR riêng biệt trước khi tích hợp vào game
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

REM Tạo thư mục test nếu chưa có
if not exist "test_images" (
    mkdir "test_images"
    echo 📁 Created test_images folder
)

if not exist "ocr_debug_output" (
    mkdir "ocr_debug_output"
    echo 📁 Created ocr_debug_output folder
)

echo.
echo 🚀 Starting OCR Debug Console...
echo.

REM Chạy OCR debug console
dotnet run --project . --configuration Debug

pause
