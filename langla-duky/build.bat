@echo off
echo ====================================
echo   Build Làng Lá Duke Tool
echo ====================================
echo.

cd /d "%~dp0"

echo Đang restore packages...
dotnet restore

if %ERRORLEVEL% NEQ 0 (
    echo Lỗi restore packages!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Đang build project...
dotnet build --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo Lỗi build project!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Build thành công!
echo File exe được tạo tại: bin\Release\net8.0-windows\
echo.

pause
