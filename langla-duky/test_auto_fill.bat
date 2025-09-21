@echo off
echo ========================================
echo Testing Auto-Fill Captcha System
echo ========================================
echo.
echo This script will test the auto-fill functionality
echo Make sure the game is running with captcha dialog open
echo.
pause

echo Starting test...
dotnet run --project langla-duky.csproj

echo.
echo Test completed. Check the logs above for auto-fill results.
pause
