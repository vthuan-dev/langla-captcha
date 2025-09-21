@echo off
echo ========================================
echo    Testing Center Captcha Detection
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
echo Starting application with improved center-focused captcha detection...
echo.
echo Expected improvements:
echo - ROI detection will focus on CENTER area (40%%-60%% from left, 30%%-70%% from top)
echo - Will AVOID top-right corner detection (X=863, Y=0)
echo - Should find captcha in the middle of game screen
echo - Higher confidence scores for center regions
echo.

dotnet run --configuration Release

echo.
echo Test completed. Check the logs above for:
echo - "Center-Focused Detection" method being used
echo - ROI coordinates in CENTER area (not X=863, Y=0)
echo - Higher confidence scores
echo - Better captcha text recognition
echo.
pause
