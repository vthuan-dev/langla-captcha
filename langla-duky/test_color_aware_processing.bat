@echo off
echo ========================================
echo Testing NEW Color-Aware Processing
echo ========================================
echo.
echo This will test the new Color-Aware processing flow
echo for colorful captchas like 'jsjx'
echo.
echo Starting application...
echo.

cd /d "%~dp0"
start "" "langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Select a game window with colorful captcha
echo 2. Click "Test OCR" button
echo 3. Check if Color-Aware processing works
echo 4. Look for "Color-Aware method result" in logs
echo 5. Verify captcha is solved correctly (jsjx)
echo.
echo Expected improvements:
echo - Better handling of colorful captchas
echo - Character segmentation in color space
echo - Selective binarization per character
echo - Higher accuracy for jsjx captcha
echo.
pause
