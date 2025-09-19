@echo off
echo ===== Restarting Langla-Duky with Null Reference Fixes =====
echo.
echo Cleaning up previous runs...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
echo.

echo Building project with null reference fixes...
dotnet build
echo.

echo Copying tessdata files to output directory...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Starting application with null reference fixes...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Done! The application should now be running with:
echo  - Fixed accessibility issues in ScreenCapture methods
echo  - Fixed null reference issues in CaptchaMonitoringService
echo  - Fixed null reference issues in WindowFinder
echo  - Fixed null reference issues in MainForm
echo.
echo If you encounter any issues, check the debug folders:
echo  - bin\Debug\net8.0-windows\ocr_debug_output
echo  - bin\Debug\net8.0-windows\captcha_detections
echo  - bin\Debug\net8.0-windows\debug_images
echo.
echo Press any key to exit...
pause > nul
