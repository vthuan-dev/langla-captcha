@echo off
echo Stopping langla-duky process...
taskkill /f /im langla-duky.exe 2>nul
timeout /t 2 /nobreak >nul

echo Building with image resize fix...
dotnet build langla-duky.csproj

if %ERRORLEVEL% EQU 0 (
    echo Build successful! Starting application...
    start "" "bin\Debug\net8.0-windows\langla-duky.exe"
) else (
    echo Build failed!
    pause
)
