# Copy Tessdata to Bin Directory
Write-Host "Copying tessdata to bin directory..." -ForegroundColor Yellow

$sourceDir = Join-Path $PSScriptRoot "tessdata"
$targetDir = Join-Path $PSScriptRoot "bin\Debug\net8.0-windows\tessdata"

Write-Host "Source: $sourceDir" -ForegroundColor Cyan
Write-Host "Target: $targetDir" -ForegroundColor Cyan

if (-not (Test-Path $sourceDir)) {
    Write-Host "❌ Source tessdata directory not found!" -ForegroundColor Red
    exit 1
}

# Create target directory
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force
    Write-Host "✅ Created target directory: $targetDir" -ForegroundColor Green
}

# Copy files
try {
    Copy-Item "$sourceDir\*" $targetDir -Recurse -Force
    Write-Host "✅ Copied tessdata files successfully!" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Files in target directory:" -ForegroundColor Cyan
    Get-ChildItem $targetDir | ForEach-Object {
        Write-Host "  - $($_.Name) ($([math]::Round($_.Length/1MB, 2)) MB)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Failed to copy tessdata: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ Tessdata setup complete!" -ForegroundColor Green
Read-Host "Press Enter to continue"
