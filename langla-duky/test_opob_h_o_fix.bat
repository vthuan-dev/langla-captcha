@echo off
echo ========================================
echo Testing OPOB H/O Confusion Fix
echo ========================================
echo.
echo Fixes implemented:
echo 1. Character correction for opob pattern (hpob → opob)
echo 2. H/O confusion handling (h, H → o)
echo 3. Enhanced preprocessing for h/o distinction
echo 4. Horizontal morphological operations
echo 5. Multiple pattern recognition
echo.
echo Starting application...
echo.

cd /d "%~dp0"
start "" "langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Select a game window with captcha 'opob'
echo 2. Click "Test OCR" button
echo 3. Check for improved results:
echo    - Character correction: hpob → opob
echo    - H/O confusion handling
echo    - Enhanced preprocessing for h/o distinction
echo    - Better morphological operations
echo.
echo Expected improvements:
echo - hpob should be corrected to opob
echo - Better distinction between h and o
echo - Enhanced preprocessing for horizontal characters
echo - Higher accuracy for h/o confusion cases
echo.
echo Debug images location:
echo D:\tool\langla-duky\langla-duky\bin\Debug\net8.0-windows\captcha_debug\
echo.
pause
