@echo off
echo ========================================
echo FORCE RESTART WITH NEW CONFIG
echo ========================================
echo.

REM Kill any running instance
taskkill /F /IM langla-duky.exe 2>nul
timeout /t 2 >nul

REM Delete old config cache if exists
del /Q "%TEMP%\langla-duky-config-cache*" 2>nul

REM Copy config to ensure it's updated
copy /Y config.json bin\Debug\net8.0-windows\config.json

echo.
echo Starting with new config...
echo Y=260, H=40 (captcha area fixed)
echo.

cd bin\Debug\net8.0-windows
start langla-duky.exe
