@echo off
echo ===================================
echo Duke Client Captcha Processor
echo ===================================
echo.
echo Restarting with Duke Client configuration...
echo.

REM Kill any existing instances
taskkill /f /im langla-duky.exe >nul 2>&1

REM Ensure tessdata is available
if not exist "tessdata" (
    echo Creating tessdata directory...
    mkdir tessdata
)

REM Check if tessdata files exist
if not exist "tessdata\eng.traineddata" (
    echo Copying tessdata files...
    powershell -ExecutionPolicy Bypass -File "copy_tessdata_to_bin.ps1"
)

REM Run the application
echo Starting application with Duke Client configuration...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"

echo.
echo Application started with Duke Client configuration!
echo.
echo Instructions:
echo 1. Click "Chọn cửa sổ" to select the Duke Client window
echo 2. Click "🎮 Duke Captcha" to process the captcha
echo.
echo ===================================
