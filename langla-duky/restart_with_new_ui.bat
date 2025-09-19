@echo off
echo ===== Restarting Langla-Duky with Enhanced UI and Captcha Detection =====
echo.
echo Cleaning up previous runs...
if exist "bin\Debug\net8.0-windows\ocr_debug_output" rmdir /s /q "bin\Debug\net8.0-windows\ocr_debug_output"
if exist "bin\Debug\net8.0-windows\captcha_detections" rmdir /s /q "bin\Debug\net8.0-windows\captcha_detections"
if exist "bin\Debug\net8.0-windows\debug_images" rmdir /s /q "bin\Debug\net8.0-windows\debug_images"
echo.

echo Building project with enhanced features...
dotnet build
echo.

echo Copying tessdata files to output directory...
powershell -ExecutionPolicy Bypass -File copy_tessdata_to_bin.ps1
echo.

echo Starting application with enhanced UI and improved captcha detection...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"
echo.

echo Done! The application should now be running with:
echo  - Real-time game window preview
echo  - Enhanced captcha detection
echo  - Multiple OCR approaches
echo  - Improved debug visualization
echo.
echo If you encounter any issues, check the debug folders:
echo  - bin\Debug\net8.0-windows\ocr_debug_output
echo  - bin\Debug\net8.0-windows\captcha_detections
echo  - bin\Debug\net8.0-windows\debug_images
echo.
echo Press any key to exit...
pause > nul