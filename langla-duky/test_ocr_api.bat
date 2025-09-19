@echo off
echo ========================================
echo    TEST OCR API - OCR.space
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] Building project...
dotnet build langla-duky.csproj --configuration Debug
if %ERRORLEVEL% neq 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)
echo ✅ Build successful
echo.

echo [2/4] Running OCR API test...
echo.
dotnet run --project TestOCRStandalone.csproj
echo.

echo [3/4] Test completed
echo.

echo [4/4] Press any key to exit...
pause >nul