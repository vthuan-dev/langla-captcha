@echo off
echo ========================================
echo    Testing Captcha Detection Improvements
echo ========================================
echo.

echo Building project...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Starting application with improved captcha detection...
echo.
echo Expected improvements:
echo - Better ROI detection for small captcha regions
echo - Higher OCR accuracy for colorful captcha like "jClO"
echo - More precise captcha area detection
echo.

dotnet run --configuration Release

echo.
echo Test completed. Check the logs above for:
echo - ROI Detection confidence > 70%%
echo - OCR result closer to actual captcha text
echo - Better captcha area detection
echo.
pause
