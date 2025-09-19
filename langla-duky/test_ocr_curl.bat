@echo off
echo ========================================
echo    TEST OCR API - CURL Method
echo ========================================
echo.

cd /d "%~dp0"

echo [1/3] Creating test image...
echo.

# Tạo ảnh test bằng PowerShell
powershell -Command "
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap(200, 50)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([System.Drawing.Color]::White)
$font = New-Object System.Drawing.Font('Arial', 20, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Black)
$graphics.DrawString('TEST', $font, $brush, 10, 10)
$graphics.Dispose()
$bitmap.Save('test_image.png', [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
Write-Host '✅ Đã tạo ảnh test: test_image.png'
"

echo.
echo [2/3] Testing OCR API with CURL...
echo.

# Test với curl (nếu có)
curl --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo ✅ CURL found, testing API...
    curl -X POST "https://api.ocr.space/parse/image" ^
         -H "apikey: K87601025288957" ^
         -F "file=@test_image.png" ^
         -F "language=eng" ^
         -F "isOverlayRequired=true" ^
         -F "detectOrientation=true" ^
         -F "scale=true" ^
         -F "OCREngine=1"
    echo.
    echo ✅ CURL test completed
) else (
    echo ❌ CURL not found, using PowerShell method...
    call test_ocr_powershell.bat
)

echo.
echo [3/3] Cleaning up...
if exist test_image.png del test_image.png
echo ✅ Cleanup completed

echo.
echo Press any key to exit...
pause >nul
