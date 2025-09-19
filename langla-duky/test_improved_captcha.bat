@echo off
echo ============================================
echo Testing Improved Captcha Detection
echo ============================================

echo.
echo 📋 Changes made in this version:
echo ✅ Thu nhỏ captcha area: 420x240 (200x30) thay vì 390x230 (320x40)  
echo ✅ Thêm Free OCR API (không cần API key)
echo ✅ Cải thiện color analysis để detect full 'dgvw'
echo ✅ Expanded color detection ranges
echo ✅ Smarter fallback logic
echo.

echo Building project...
dotnet build -q

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

echo.
echo 📋 Current captcha area settings:
echo X=420, Y=240, Width=200, Height=30
echo.

echo 🎯 Expected improvements:
echo 1. Less background noise (smaller capture area)
echo 2. Better color detection (expanded ranges)
echo 3. More OCR services (OCR.space + Free API + Tesseract)
echo 4. Smarter guessing (if brown dominant, assume 'dgvw')
echo.

echo 📝 Test plan:
echo 1. Make sure Duke Client captcha dialog is open
echo 2. Run One-shot detection
echo 3. Check logs for:
echo    - "Color detection: Brown=True, Yellow=True, Purple=True, Green=True" 
echo    - "✅ Detected full Duke Client 4-color pattern - guessing 'dgvw'"
echo    - "Free OCR result: 'dgvw'" (if API works)
echo.

pause
echo.
echo Running the improved tool...
dotnet run

echo.
echo 📊 Analysis questions:
echo - Did it detect more colors now?
echo - Did color analysis show 'dgvw' instead of just 'd'?  
echo - Did Free OCR API work?
echo - Was the captcha successful?
echo.
pause
