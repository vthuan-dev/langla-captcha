@echo off
echo Testing fixed tool...
echo.
echo Starting langla-duky with fixes:
echo - Fixed duplicate key 'I' error
echo - Disabled aggressive a/g character replacement
echo - Improved OCR result handling
echo.
cd /d "%~dp0"
dotnet run
pause
