@echo off
echo ====================================
echo   Restart Tool với Tessdata Fix
echo ====================================
echo.

echo Đang dừng tool hiện tại...
taskkill /F /IM langla-duky.exe 2>nul

echo Đang đợi 2 giây...
timeout /t 2 /nobreak >nul

echo Đang khởi động lại tool...
cd /d "%~dp0"
dotnet run

pause
