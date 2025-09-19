# Download Tessdata from Internet
Write-Host "Downloading tessdata from internet..." -ForegroundColor Yellow

$tessdataUrl = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
$tessdataDir = Join-Path $PSScriptRoot "tessdata"
$tessdataFile = Join-Path $tessdataDir "eng.traineddata"

# Create tessdata directory
if (-not (Test-Path $tessdataDir)) {
    New-Item -ItemType Directory -Path $tessdataDir -Force
    Write-Host "Created tessdata directory: $tessdataDir" -ForegroundColor Green
}

# Download eng.traineddata
try {
    Write-Host "Downloading eng.traineddata..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri $tessdataUrl -OutFile $tessdataFile -UseBasicParsing
    Write-Host "Downloaded eng.traineddata successfully!" -ForegroundColor Green
    Write-Host "File location: $tessdataFile" -ForegroundColor Green
} catch {
    Write-Host "Failed to download tessdata: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please download manually from: https://github.com/tesseract-ocr/tessdata" -ForegroundColor Yellow
}

Write-Host ""
Read-Host "Press Enter to continue"
