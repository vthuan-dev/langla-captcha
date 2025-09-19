# Quick Fix Tessdata - PowerShell Version
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "   Quick Fix Tessdata" -ForegroundColor Cyan  
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Các vị trí có thể có Tesseract
$possiblePaths = @(
    "C:\Program Files\Tesseract-OCR\tessdata",
    "C:\Program Files (x86)\Tesseract-OCR\tessdata", 
    "C:\tesseract\tessdata",
    "$env:USERPROFILE\AppData\Local\Tesseract-OCR\tessdata"
)

Write-Host "Đang tìm Tesseract tessdata..." -ForegroundColor Yellow

$foundPath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        Write-Host "✓ Tìm thấy tessdata tại: $path" -ForegroundColor Green
        $foundPath = $path
        break
    }
}

if (-not $foundPath) {
    Write-Host "❌ Không tìm thấy tessdata!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Vui lòng:" -ForegroundColor Yellow
    Write-Host "1. Cài đặt Tesseract OCR từ: https://github.com/UB-Mannheim/tesseract/wiki" -ForegroundColor White
    Write-Host "2. Hoặc chạy: auto_install_tesseract.bat" -ForegroundColor White
    Write-Host ""
    Read-Host "Nhấn Enter để thoát"
    exit 1
}

# Copy tessdata
$targetPath = Join-Path $PSScriptRoot "tessdata"
Write-Host "Đang copy tessdata..." -ForegroundColor Yellow

if (Test-Path $targetPath) {
    Write-Host "Thư mục tessdata đã tồn tại, đang xóa..." -ForegroundColor Yellow
    Remove-Item $targetPath -Recurse -Force
}

try {
    Copy-Item $foundPath $targetPath -Recurse -Force
    Write-Host ""
    Write-Host "✅ Copy tessdata thành công!" -ForegroundColor Green
    Write-Host "Thư mục tessdata đã được tạo tại: $targetPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Bây giờ bạn có thể chạy tool captcha automation." -ForegroundColor Cyan
} catch {
    Write-Host ""
    Write-Host "❌ Lỗi copy tessdata!" -ForegroundColor Red
    Write-Host "Vui lòng copy thủ công từ $foundPath đến $targetPath" -ForegroundColor White
}

Write-Host ""
Read-Host "Nhấn Enter để thoát"
