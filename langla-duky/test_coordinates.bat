@echo off
echo =====================================
echo   Test Langla-Duky with New Coords
echo =====================================
echo.
echo Make sure:
echo 1. Game "Duke Client - By iamDuke" is running
echo 2. Captcha dialog is visible
echo 3. Game window is NOT minimized
echo.
echo Press any key to start test...
pause

cd /d "%~dp0\bin\Release\net8.0-windows"
langla-duky.exe

echo.
echo Test completed!
echo Check debug_images folder for captured images
pause
