@echo off
echo 🔄 Restarting application with OCR debug fix...
echo.

REM Kill existing process
taskkill /f /im langla-duky.exe 2>nul
if errorlevel 1 (
    echo No existing process found
) else (
    echo Killed existing process
)

REM Wait a moment
timeout /t 2 /nobreak >nul

REM Create debug folder in both locations
echo Creating debug folders...
mkdir ocr_debug_output 2>nul
mkdir bin\Debug\net8.0-windows\ocr_debug_output 2>nul
echo Debug folders created

REM Start application
echo Starting application with OCR debug fix...
dotnet run --configuration Debug

pause
