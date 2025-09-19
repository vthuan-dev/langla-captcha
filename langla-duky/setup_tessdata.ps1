# Setup Tesseract Data for OpenCV + Tesseract Integration
# This script downloads the required tessdata file

Write-Host "🔧 Setting up Tesseract data for OpenCV + Tesseract integration..." -ForegroundColor Green

# Create tessdata directory
$tessdataDir = "tessdata"
if (!(Test-Path $tessdataDir)) {
    New-Item -ItemType Directory -Path $tessdataDir
    Write-Host "📁 Created tessdata directory" -ForegroundColor Yellow
}

# Download eng.traineddata if not exists
$engFile = "$tessdataDir\eng.traineddata"
if (!(Test-Path $engFile)) {
    Write-Host "📥 Downloading eng.traineddata..." -ForegroundColor Yellow
    try {
        $url = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
        Invoke-WebRequest -Uri $url -OutFile $engFile
        Write-Host "✅ Downloaded eng.traineddata successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to download eng.traineddata: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "💡 Please manually download from: https://github.com/tesseract-ocr/tessdata" -ForegroundColor Cyan
        Write-Host "   Save as: $engFile" -ForegroundColor Cyan
    }
} else {
    Write-Host "✅ eng.traineddata already exists" -ForegroundColor Green
}

# Check file size
if (Test-Path $engFile) {
    $fileSize = (Get-Item $engFile).Length / 1MB
    Write-Host "📊 File size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
    
    if ($fileSize -lt 1) {
        Write-Host "⚠️ File seems too small, may be corrupted" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎯 Setup complete! You can now use OpenCV + Tesseract captcha solver" -ForegroundColor Green
Write-Host "📁 Tessdata location: $(Resolve-Path $tessdataDir)" -ForegroundColor Cyan

# Create captcha debug directories
$debugDirs = @("captcha_opencv_debug", "captcha_test", "captcha_enhanced")
foreach ($dir in $debugDirs) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir
        Write-Host "📁 Created debug directory: $dir" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🚀 Ready to test! Use 'Test OpenCV' button in the application" -ForegroundColor Green
