@echo off
echo 🔄 Restarting application with path fix...
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

REM Start application
echo Starting application with path fix...
dotnet run --configuration Debug

pause
