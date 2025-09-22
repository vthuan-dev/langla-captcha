@echo off
echo ========================================
echo Testing Continuous Captcha Monitoring
echo ========================================
echo.
echo This test verifies the new continuous monitoring feature:
echo 1. Start monitoring mode
echo 2. Tool runs continuously in background
echo 3. Automatically detects when captcha appears
echo 4. Solves and fills captcha automatically
echo 5. Returns to monitoring mode
echo.
echo Expected behavior:
echo - Tool monitors captcha area every 2-5 seconds
echo - Automatically detects captcha appearance
echo - Solves captcha without user intervention
echo - Smart timing: fast mode when activity detected
echo.

echo Starting application...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Wait for the application to start
echo 2. Select your game window
echo 3. Click "Start Monitor" button (NOT "Start")
echo 4. Check the log for:
echo    - "🔄 Starting continuous captcha monitoring..."
echo    - "📡 Monitoring mode: Smart detection"
echo    - "✅ Continuous monitoring started successfully!"
echo 5. Play your game normally
echo 6. When captcha appears, tool should automatically:
echo    - Detect captcha: "🔍 Captcha detected! Processing..."
echo    - Solve captcha: "✅ Captcha solved: '[result]'"
echo    - Auto-fill: "🎯 Captcha auto-solved successfully!"
echo 7. Tool returns to monitoring mode
echo.
echo Features:
echo - Smart Detection: Screenshots every 2-5 seconds
echo - Adaptive Timing: Fast mode when activity detected
echo - Background Processing: Runs in separate thread
echo - Auto-Recovery: Handles errors gracefully
echo - Real-time Status: Shows current monitoring state
echo.
echo To stop monitoring: Click "Stop Monitor" button
echo.
pause
