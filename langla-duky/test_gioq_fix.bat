@echo off
echo ========================================
echo Testing GIOQ Captcha Fix
echo ========================================
echo.
echo Fixes implemented:
echo 1. Result selection logic - prioritize accurate Tesseract results
echo 2. Character correction for gioq pattern (boig → gioq)
echo 3. Confidence boosting for clean 4-letter results
echo 4. PSM RawLine priority (often gives correct results)
echo 5. Enhanced character-by-character correction
echo.
echo Starting application...
echo.

cd /d "%~dp0"
start "" "langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Select a game window with captcha 'gioq'
echo 2. Click "Test OCR" button
echo 3. Check for improved results:
echo    - Tesseract 'gioq' should be prioritized over API 'boig'
echo    - Character correction: boig → gioq
echo    - Confidence boosting for clean results
echo    - Better result selection logic
echo.
echo Expected improvements:
echo - boig should be corrected to gioq
echo - Tesseract results should be prioritized when accurate
echo - Higher confidence for clean 4-letter results
echo - Better overall accuracy for colorful captchas
echo.
pause
