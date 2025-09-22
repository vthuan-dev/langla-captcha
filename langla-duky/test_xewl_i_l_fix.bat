@echo off
echo ========================================
echo Testing XEWL I/L Confusion Fix
echo ========================================
echo.
echo Fixes implemented:
echo 1. Character correction for xewl pattern (xewi → xewl)
echo 2. I/L confusion handling (I, 1, |, L → l)
echo 3. Enhanced preprocessing for i/l distinction
echo 4. Vertical morphological operations
echo 5. Position-based character correction
echo.
echo Starting application...
echo.

cd /d "%~dp0"
start "" "langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Select a game window with captcha 'xewl'
echo 2. Click "Test OCR" button
echo 3. Check for improved results:
echo    - Character correction: xewi → xewl
echo    - I/L confusion handling
echo    - Enhanced preprocessing for i/l distinction
echo    - Better morphological operations
echo.
echo Expected improvements:
echo - xewi should be corrected to xewl
echo - Better distinction between i and l
echo - Enhanced preprocessing for vertical characters
echo - Higher accuracy for i/l confusion cases
echo.
pause
