# Hiển thị banner
Write-Host "=== Khởi chạy Làng Lá Duke - Captcha Automation Tool với Tesseract OCR ===" -ForegroundColor Cyan
Write-Host ""

try {
    # Bước 1: Cập nhật tessdata
    Write-Host "[1/3] Đang cập nhật tessdata..." -ForegroundColor Yellow
    & "$PSScriptRoot\update_tessdata.bat"
    if ($LASTEXITCODE -ne 0) {
        throw "Lỗi khi cập nhật tessdata"
    }
    Write-Host "✅ Cập nhật tessdata thành công!" -ForegroundColor Green
    Write-Host ""

    # Bước 2: Build project
    Write-Host "[2/3] Đang build project..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        throw "Lỗi khi build project"
    }
    Write-Host "✅ Build project thành công!" -ForegroundColor Green
    Write-Host ""

    # Bước 3: Chạy ứng dụng
    Write-Host "[3/3] Khởi động ứng dụng..." -ForegroundColor Yellow
    Write-Host "🚀 Ứng dụng đang chạy..." -ForegroundColor Cyan
    Write-Host ""
    dotnet run
}
catch {
    Write-Host "❌ Lỗi: $_" -ForegroundColor Red
    Write-Host "Stack trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
}
finally {
    Write-Host ""
    Write-Host "Nhấn phím bất kỳ để thoát..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
