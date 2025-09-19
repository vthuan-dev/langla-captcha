@echo off
echo 🔍 Testing OCR Direct...
echo.

REM Kill existing process
taskkill /f /im langla-duky.exe 2>nul

REM Wait
timeout /t 2 /nobreak >nul

REM Build
echo Building...
dotnet build --configuration Debug

REM Run direct test
echo Running direct OCR test...
dotnet run --configuration Debug -- --direct-test

pause
