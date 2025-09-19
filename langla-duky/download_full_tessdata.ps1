# Download Additional Tessdata Files
Write-Host "Downloading additional tessdata files..." -ForegroundColor Yellow

$tessdataDir = Join-Path $PSScriptRoot "tessdata"

# Files to download
$files = @{
    "osd.traineddata" = "https://github.com/tesseract-ocr/tessdata/raw/main/osd.traineddata"
    "eng.traineddata" = "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata"
}

foreach ($file in $files.GetEnumerator()) {
    $filePath = Join-Path $tessdataDir $file.Key
    Write-Host "Downloading $($file.Key)..." -ForegroundColor Yellow
    
    try {
        Invoke-WebRequest -Uri $file.Value -OutFile $filePath -UseBasicParsing
        Write-Host "✓ Downloaded $($file.Key) successfully!" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to download $($file.Key): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Tessdata files:" -ForegroundColor Cyan
Get-ChildItem $tessdataDir | ForEach-Object {
    Write-Host "  - $($_.Name) ($([math]::Round($_.Length/1MB, 2)) MB)" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to continue"
