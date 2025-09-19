@echo off
echo ===== Restarting Langla-Duky with Captcha Fix =====
echo.
echo Cleaning up previous runs...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
echo.

echo Building project...
dotnet build
echo.

echo Copying tessdata files to output directory...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Starting application with enhanced captcha detection...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Done! The application should now be running with improved captcha detection.
echo If you encounter any issues, check the debug folders:
echo - bin\Debug\net8.0-windows\ocr_debug_output
echo - bin\Debug\net8.0-windows\captcha_detections
echo.
echo Press any key to exit...
pause > nul