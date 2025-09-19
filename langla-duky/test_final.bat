@echo off
echo =========================================
echo   FINAL TEST - All Improvements Applied
echo =========================================
echo.
echo MAJOR IMPROVEMENTS:
echo 1. Better coordinates (X:513, Y:263, W:200, H:70)
echo 2. Pink text isolation algorithm
echo 3. Improved OCR character whitelisting
echo 4. 2.5x image scaling
echo 5. Coordinate Finder tool added
echo 6. Debug image saving enabled
echo.
echo INSTRUCTIONS:
echo 1. Make sure game shows captcha dialog
echo 2. Click "Find Coords" button to verify coordinates
echo 3. Click "Test OCR" to test once
echo 4. Check debug_images folder if needed
echo.
echo Starting tool...
echo.

cd /d "%~dp0\bin\Release\net8.0-windows"
langla-duky.exe

echo.
pause
