@echo off
echo ========================================
echo Testing IMPROVED OpenCV Processing Flow
echo ========================================
echo.
echo OpenCV Flow Improvements:
echo 1. Proper noise removal BEFORE scaling
echo 2. Contrast enhancement with histogram equalization
echo 3. Multiple thresholding methods (Adaptive, Otsu, Fixed)
echo 4. Best method selection based on character density
echo 5. Enhanced morphological operations
echo 6. Character correction for 'i see' pattern
echo.
echo Starting application...
echo.

cd /d "%~dp0"
start "" "langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Select a game window with captcha 'i see'
echo 2. Click "Test OCR" button
echo 3. Check for improved results:
echo    - Better noise removal with Gaussian blur
echo    - Contrast enhancement with histogram equalization
echo    - Multiple thresholding methods comparison
echo    - Character correction: cciü → i see
echo    - Enhanced morphological operations
echo.
echo Expected improvements:
echo - cciü should be corrected to i see
echo - Better image quality with proper OpenCV flow
echo - Higher accuracy for noisy captchas
echo - More debug images for analysis
echo.
pause
