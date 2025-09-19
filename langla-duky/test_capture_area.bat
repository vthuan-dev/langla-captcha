@echo off
echo ==========================================
echo   TEST CAPTURE AREA - SEE WHAT'S CAPTURED
echo ==========================================
echo.
echo This will capture the current area and save to debug_images
echo.
echo STEPS:
echo 1. Open game with captcha visible
echo 2. Run this test
echo 3. Check debug_images folder
echo 4. See what was actually captured
echo 5. Adjust coordinates if needed
echo.
echo Press any key to start capture test...
pause

cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File find_captcha_coords.ps1

echo.
echo Done! Check the coordinates and update config.json
pause
