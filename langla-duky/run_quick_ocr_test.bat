@echo off
echo 🔍 Running Quick OCR Test...
echo.

REM Kill existing process
taskkill /f /im langla-duky.exe 2>nul

REM Wait
timeout /t 2 /nobreak >nul

REM Build
echo Building...
dotnet build --configuration Debug

REM Run quick test
echo Running quick OCR test...
dotnet run --configuration Debug -- --quick-test

pause
