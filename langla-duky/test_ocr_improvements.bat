@echo off
echo Testing OCR improvements with pattern corrections...
echo.
echo Features:
echo - Added most common OCR character confusions
echo - Prioritized pattern-corrected results
echo - Fixed 'y' vs 'n' confusion (yurq pattern)
echo - Added lowercase-only character corrections
echo.
cd /d "%~dp0"
dotnet run
pause
