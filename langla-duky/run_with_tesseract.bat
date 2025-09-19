@echo off
echo === Khởi chạy Làng Lá Duke - Captcha Automation Tool với Tesseract OCR ===
echo.

REM Chạy update_tessdata.bat để cập nhật model
echo [1/3] Đang cập nhật tessdata...
call update_tessdata.bat

REM Build project
echo.
echo [2/3] Đang build project...
dotnet build

REM Chạy ứng dụng
echo.
echo [3/3] Khởi động ứng dụng...
dotnet run

pause
