@echo off
echo ========================================
echo Testing yzle lowercase l fix
echo ========================================
echo.
echo This test verifies that the system correctly handles:
echo - yzle captcha (y-golden, z-red, l-purple, e-blue)
echo - I/l confusion: yZIe should become yzle
echo - Z/z confusion: yZle should become yzle
echo - i/l confusion: yzie should become yzle
echo.
echo Expected result: yzle (not yzie)
echo.

echo Starting application...
start "" "bin\Debug\net8.0-windows\langla-duky.exe"

echo.
echo ========================================
echo Test Instructions:
echo ========================================
echo 1. Wait for the application to start
echo 2. Click "Test" button to test captcha solving
echo 3. Check if the result is 'yzle' instead of 'yzie'
echo 4. Look for these corrections in the log:
echo    - "yZIe" should be corrected to "yzle"
echo    - "yZle" should be corrected to "yzle" 
echo    - "yzie" should be corrected to "yzle"
echo.
echo The fix includes:
echo - Pattern corrections for yzle captcha
echo - Character-by-character corrections for Z/z and i/l
echo - Improved I/l confusion handling for lowercase captchas
echo.
pause
