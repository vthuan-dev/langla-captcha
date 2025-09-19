@echo off
echo 🔄 Restarting application with enhanced OCR debug logging...
echo.

REM Kill existing process
taskkill /f /im langla-duky.exe 2>nul
if errorlevel 1 (
    echo No existing process found
) else (
    echo Killed existing process
)

REM Wait a moment
timeout /t 3 /nobreak >nul

REM Build and run
echo Building application with enhanced logging...
dotnet build --configuration Debug
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo Starting application with enhanced OCR debug logging...
dotnet run --configuration Debug

pause
