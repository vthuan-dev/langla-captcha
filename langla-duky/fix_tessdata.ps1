# Quick Fix Tessdata
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "   Quick Fix Tessdata" -ForegroundColor Cyan  
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Cac vi tri co the co Tesseract
$possiblePaths = @(
    "C:\Program Files\Tesseract-OCR\tessdata",
    "C:\Program Files (x86)\Tesseract-OCR\tessdata", 
    "C:\tesseract\tessdata"
)

Write-Host "Dang tim Tesseract tessdata..." -ForegroundColor Yellow

$foundPath = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        Write-Host "Tim thay tessdata tai: $path" -ForegroundColor Green
        $foundPath = $path
        break
    }
}

if (-not $foundPath) {
    Write-Host "Khong tim thay tessdata!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Vui long:" -ForegroundColor Yellow
    Write-Host "1. Cai dat Tesseract OCR tu: https://github.com/UB-Mannheim/tesseract/wiki" -ForegroundColor White
    Write-Host "2. Hoac chay: auto_install_tesseract.bat" -ForegroundColor White
    Write-Host ""
    Read-Host "Nhan Enter de thoat"
    exit 1
}

# Copy tessdata
$targetPath = Join-Path $PSScriptRoot "tessdata"
Write-Host "Dang copy tessdata..." -ForegroundColor Yellow

if (Test-Path $targetPath) {
    Write-Host "Thu muc tessdata da ton tai, dang xoa..." -ForegroundColor Yellow
    Remove-Item $targetPath -Recurse -Force
}

try {
    Copy-Item $foundPath $targetPath -Recurse -Force
    Write-Host ""
    Write-Host "Copy tessdata thanh cong!" -ForegroundColor Green
    Write-Host "Thu muc tessdata da duoc tao tai: $targetPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "Bay gio ban co the chay tool captcha automation." -ForegroundColor Cyan
} catch {
    Write-Host ""
    Write-Host "Loi copy tessdata!" -ForegroundColor Red
    Write-Host "Vui long copy thu cong tu $foundPath den $targetPath" -ForegroundColor White
}

Write-Host ""
Read-Host "Nhan Enter de thoat"
