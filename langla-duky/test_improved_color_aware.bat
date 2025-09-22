@echo off
echo ========================================
echo Testing IMPROVED Color-Aware Processing
echo ========================================
echo.
echo Improvements made:
echo - Enhanced color detection with specific color ranges
echo - Added wwgt pattern correction (wvmr → wwgt)
echo - Optimized Tesseract settings (SingleChar first)
echo - Increased scaling to 16x for better quality
echo - Improved adaptive threshold settings
echo.
echo Starting application...
echo.

cd /d "%~dp0"
start "" "langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Select a game window with colorful captcha (wwgt)
echo 2. Click "Test OCR" button
echo 3. Check for improved results:
echo    - Better color detection with specific masks
echo    - Character correction: wvmr → wwgt
echo    - Higher quality processing with 16x scaling
echo    - SingleChar PSM mode for better character recognition
echo.
echo Expected improvements:
echo - wvmr should be corrected to wwgt
echo - Better character segmentation
echo - Higher accuracy for colorful captchas
echo.
pause
