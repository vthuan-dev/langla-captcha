@echo off
echo 🔍 Testing OCR directly from console...
echo.

REM Check if test image exists
if not exist "test_image\ocr-1.png" (
    echo ❌ Test image not found: test_image\ocr-1.png
    pause
    exit /b 1
)

echo ✅ Test image found: test_image\ocr-1.png

REM Create debug folder
if not exist "ocr_debug_output" mkdir ocr_debug_output
echo 📁 Debug folder created/verified

REM Run simple OCR test
echo 🔧 Running simple OCR test...
dotnet run --configuration Debug --project . -- --test-ocr

pause
