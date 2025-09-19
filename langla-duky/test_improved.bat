@echo off
echo =========================================
echo   Testing Improved Captcha Detection
echo =========================================
echo.
echo IMPROVEMENTS MADE:
echo ✅ Updated captcha area coordinates
echo ✅ Enhanced image preprocessing (3x scaling)
echo ✅ Better OCR settings
echo ✅ Adaptive thresholding
echo ✅ Debug image saving
echo.
echo Make sure:
echo 1. Game shows captcha dialog
echo 2. Captcha is visible (like "oknY")
echo 3. Game window is active
echo.
echo Press any key to test...
pause

cd /d "%~dp0\bin\Release\net8.0-windows"

echo.
echo Starting tool...
langla-duky.exe

echo.
echo =========================================
echo Check debug_images folder for captured images
echo if tool still reads wrong text.
echo =========================================
pause
