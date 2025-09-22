@echo off
echo Testing API key rotation and rate limit handling...
echo.
echo Features:
echo - Automatic API key switching on rate limit
echo - API call count tracking
echo - Tesseract fallback when all APIs are rate limited
echo - Improved logging for API key switches
echo.
cd /d "%~dp0"
dotnet run
pause
