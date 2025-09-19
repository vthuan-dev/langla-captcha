@echo off
echo ========================================
echo Debug Image Viewer for Captcha Issues
echo ========================================

echo.
echo This tool will help you examine the actual captcha images being captured.
echo.

if not exist "bin\Debug\net8.0-windows\captcha_debug\" (
    echo ❌ No debug folder found: bin\Debug\net8.0-windows\captcha_debug\
    echo Make sure you've run the tool at least once to generate debug images.
    pause
    exit /b 1
)

echo 📂 Looking for latest debug images...

cd bin\Debug\net8.0-windows\captcha_debug\

echo.
echo 📋 Latest captcha debug images:
dir /b /o-d captcha_area_*.png | head -5

echo.
echo 📋 Latest processed images:
dir /b /o-d captcha_processed_*.png | head -5

echo.
echo 🔍 Instructions:
echo 1. Look at the images above in Windows Explorer or any image viewer
echo 2. Check if the captcha_area images actually contain text
echo 3. Check if the captcha_processed images are readable
echo 4. If images are blank or wrong area, coordinates need adjustment
echo 5. If images contain text but OCR fails, OCR settings need tweaking
echo.

echo 📂 Opening debug folder in Explorer...
start .

echo.
echo Also check coordinate debug folder for full window captures:
if exist "..\coordinate_debug\" (
    echo 📂 Opening coordinate debug folder...
    start ..\coordinate_debug\
) else (
    echo ❌ No coordinate debug folder found
)

echo.
echo ✅ Debug folders opened. Examine the images and see:
echo   - Are the images capturing the right area?
echo   - Do the images actually contain captcha text?
echo   - Are the colors/contrast good enough for OCR?
echo.
pause
