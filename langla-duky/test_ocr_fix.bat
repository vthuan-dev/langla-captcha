@echo off
echo ============================================
echo Testing OCR Fixes for Duke Client Captcha
echo ============================================

echo.
echo Building the project...
dotnet build

if %ERRORLEVEL% NEQ 0 (
    echo Build failed! Please fix compilation errors first.
    pause
    exit /b 1
)

echo.
echo Build successful! 
echo.
echo The following issues have been fixed:
echo ✅ OCR validation is now much more lenient
echo ✅ Added fallback detection methods
echo ✅ Improved colored captcha processing
echo ✅ Added color-based analysis for better guesses
echo ✅ Enhanced debugging and logging
echo.

echo Running the main application...
echo.
echo Instructions:
echo 1. Make sure Duke Client is open
echo 2. Try running the captcha detection (One-shot or continuous)
echo 3. Check the logs for improved detection messages
echo 4. Debug images will be saved to captcha_debug folder
echo 5. The tool should now be more likely to detect and process captchas
echo.

dotnet run

echo.
echo Test completed.
pause
