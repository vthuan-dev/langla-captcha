@echo off
echo ===============================================
echo CapSolver Integration Test - Premium AI CAPTCHA Solver
echo ===============================================

echo.
echo 🚀 CapSolver Features:
echo ✅ Advanced AI-powered CAPTCHA recognition
echo ✅ Support for colored text like Duke Client "dgvw" 
echo ✅ Much higher accuracy than OCR.space/Tesseract
echo ✅ Fast solving (usually 10-30 seconds)
echo ✅ API key ready: CAP-C90A0F23AE0DC0C4FDEFE047059EBBF134117DB9A0A8826E9DF5752BCFFF2507
echo.

echo 💰 Pricing info:
echo - ImageToTextTask: ~$0.0005 per solve (very affordable)
echo - Much more reliable than free OCR services
echo - Account balance will be checked automatically
echo.

echo 🏗️ Building project...
dotnet build -q

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed! Please check for compilation errors.
    pause
    exit /b 1
)

echo ✅ Build successful!
echo.

echo 📋 Current settings in config.json:
echo - UseCapSolver: true
echo - CapSolverAPIKey: CAP-C90A0F23AE0DC0C4FDEFE047059EBBF134117DB9A0A8826E9DF5752BCFFF2507
echo - Captcha area: 440x245 (160x25) - optimized for Duke Client
echo.

echo 🎯 Test Instructions:
echo 1. Make sure Duke Client is open with a captcha dialog
echo 2. The tool will first try CapSolver (premium AI)
echo 3. If CapSolver fails, it will fallback to legacy OCR methods
echo 4. Watch for these log messages:
echo    - "💰 CapSolver balance: $X.XXXX"
echo    - "🎯 CapSolver SUCCESS: 'dgvw'"
echo    - "🚀 ProcessCaptcha: Using CapSolver (Premium AI CAPTCHA solver)..."
echo.

echo 📊 Expected workflow:
echo CapSolver (AI) → OCR.space → Free OCR → Tesseract → Color Analysis
echo.

pause
echo.
echo 🚀 Running CapSolver-powered captcha tool...
dotnet run

echo.
echo 📈 Test Results Analysis:
echo - Did CapSolver show your account balance?
echo - Did CapSolver successfully solve the captcha?
echo - How long did it take?
echo - Was the result accurate?
echo.

echo 💡 Tips:
echo - If balance is $0, visit capsolver.com to add funds
echo - CapSolver is much more accurate for complex captchas
echo - Tool automatically falls back to free methods if needed
echo.
pause
