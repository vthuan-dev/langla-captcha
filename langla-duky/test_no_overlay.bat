@echo off
echo Testing tool without overlay windows and disabled buttons...
echo.
echo Changes made:
echo - All buttons stay enabled (no more mờ mờ buttons)
echo - Disabled ROI overlay windows (no more transparent overlays)
echo - Monitoring still works but without UI interference
echo.
cd /d "%~dp0"
dotnet run
pause
