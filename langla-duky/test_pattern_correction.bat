@echo off
echo Testing pattern correction for 'nurq' -> 'yurq'...
echo.
echo Features:
echo - Fixed duplicate key errors
echo - Added pattern correction in PostProcessOCRResult
echo - 'nurq' will be corrected to 'yurq'
echo - Pattern corrections have highest priority
echo.
cd /d "%~dp0"
dotnet run
pause
