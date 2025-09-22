@echo off
echo Testing fixed application...
echo.

cd /d "D:\tool\langla-duky\langla-duky"

echo Building application...
dotnet build --configuration Release
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful! Starting application...
echo.

dotnet run --configuration Release

echo.
echo Application finished.
pause
