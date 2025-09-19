@echo off
echo 🔍 Running Simple Tesseract Test...
echo ====================================

cd /d "D:\tool\langla-duky\langla-duky"

echo 📁 Current directory: %CD%
echo.

echo 🚀 Building project...
dotnet build --configuration Debug
if %ERRORLEVEL% neq 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

echo.
echo 🚀 Running Simple Tesseract Test...
echo.

dotnet run --configuration Debug --project . -- SimpleTesseractTest

echo.
echo ✅ Test completed!
pause
