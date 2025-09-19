@echo off
echo ===================================
echo Fixed Event Handlers for Duke Client
echo ===================================
echo.
echo Restarting with fixed event handlers configuration...
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
echo Starting application with fixed event handlers...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"

echo.
echo Application started with fixed event handlers!
echo.
echo Instructions:
echo 1. Click "Chọn cửa sổ" to select the Duke Client window
echo 2. Click "🎮 Duke Captcha" to process captcha
echo 3. The tool will automatically detect, read, and enter the captcha
echo.
echo If you encounter any issues, check the console output for error messages.
echo.
pause
