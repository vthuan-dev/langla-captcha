@echo off
echo 🔍 Debugging OCR Test...
echo.

REM Check if test image exists
echo Checking test image...
if exist "test_image\ocr-1.png" (
    echo ✅ Found: test_image\ocr-1.png
) else (
    echo ❌ Not found: test_image\ocr-1.png
)

if exist "..\..\..\test_image\ocr-1.png" (
    echo ✅ Found: ../../../test_image/ocr-1.png
) else (
    echo ❌ Not found: ../../../test_image/ocr-1.png
)

REM Check debug folders
echo.
echo Checking debug folders...
if exist "ocr_debug_output" (
    echo ✅ Found: ocr_debug_output
    dir ocr_debug_output
) else (
    echo ❌ Not found: ocr_debug_output
)

if exist "bin\Debug\net8.0-windows\ocr_debug_output" (
    echo ✅ Found: bin\Debug\net8.0-windows\ocr_debug_output
    dir bin\Debug\net8.0-windows\ocr_debug_output
) else (
    echo ❌ Not found: bin\Debug\net8.0-windows\ocr_debug_output
)

REM Check tessdata
echo.
echo Checking tessdata...
if exist "tessdata" (
    echo ✅ Found: tessdata
    dir tessdata
) else (
    echo ❌ Not found: tessdata
)

if exist "..\..\..\tessdata" (
    echo ✅ Found: ../../../tessdata
    dir ..\..\..\tessdata
) else (
    echo ❌ Not found: ../../../tessdata
)

pause
