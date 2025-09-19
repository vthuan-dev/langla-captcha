@echo off
echo === Cập nhật tessdata cho Tesseract OCR ===
echo.

REM Tạo thư mục tessdata nếu chưa tồn tại
if not exist "tessdata" (
    echo Tạo thư mục tessdata...
    mkdir tessdata
)

REM Kiểm tra và tải các file traineddata cần thiết
echo Kiểm tra và tải các file traineddata...

REM Kiểm tra eng.traineddata
if not exist "tessdata\eng.traineddata" (
    echo Tải eng.traineddata...
    powershell -Command "& {Invoke-WebRequest -Uri 'https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata' -OutFile 'tessdata\eng.traineddata'}"
) else (
    echo eng.traineddata đã tồn tại.
)

REM Kiểm tra osd.traineddata
if not exist "tessdata\osd.traineddata" (
    echo Tải osd.traineddata...
    powershell -Command "& {Invoke-WebRequest -Uri 'https://github.com/tesseract-ocr/tessdata/raw/main/osd.traineddata' -OutFile 'tessdata\osd.traineddata'}"
) else (
    echo osd.traineddata đã tồn tại.
)

REM Không cần tải model tiếng Việt vì captcha chỉ chứa số và chữ cái
echo Model tiếng Anh (eng.traineddata) đã đủ để nhận dạng số và chữ cái.

REM Copy tessdata vào thư mục bin
echo.
echo Copy tessdata vào thư mục bin...
if not exist "bin\Debug\net8.0-windows\tessdata" (
    mkdir "bin\Debug\net8.0-windows\tessdata"
)
xcopy /Y /S /Q "tessdata\*" "bin\Debug\net8.0-windows\tessdata\"

if not exist "bin\Release\net8.0-windows\tessdata" (
    mkdir "bin\Release\net8.0-windows\tessdata"
)
xcopy /Y /S /Q "tessdata\*" "bin\Release\net8.0-windows\tessdata\"

echo.
echo === Hoàn tất cập nhật tessdata ===
echo.
pause
