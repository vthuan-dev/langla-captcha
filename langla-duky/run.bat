@echo off
echo ====================================
echo   Làng Lá Duke - Captcha Tool
echo ====================================
echo.
echo Đang khởi động tool...
echo.

cd /d "%~dp0"
dotnet run

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Lỗi khởi động tool!
    echo Kiểm tra .NET 8.0 đã được cài đặt chưa.
    pause
    exit /b %ERRORLEVEL%
)

pause
