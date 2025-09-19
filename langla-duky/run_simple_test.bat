@echo off
echo 🔍 Running Simple OCR Test...
echo.

REM Kill existing process
taskkill /f /im langla-duky.exe 2>nul

REM Wait
timeout /t 2 /nobreak >nul

REM Build
echo Building...
dotnet build --configuration Debug

REM Run simple test
echo Running simple OCR test...
dotnet run --configuration Debug -- --simple-test

pause
