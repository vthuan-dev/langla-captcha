@echo off
echo 🔍 Testing Quick OCR Test directly...
echo.

REM Kill existing process
taskkill /f /im langla-duky.exe 2>nul

REM Wait
timeout /t 2 /nobreak >nul

REM Build
echo Building...
dotnet build --configuration Debug

REM Run quick test
echo Running Quick OCR Test...
dotnet run --configuration Debug -- --quick-test

pause
