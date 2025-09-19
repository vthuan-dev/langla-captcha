Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   CAPTCHA COORDINATE FINDER" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will help you find the exact coordinates of the captcha box." -ForegroundColor Yellow
Write-Host ""
Write-Host "INSTRUCTIONS:" -ForegroundColor Green
Write-Host "1. Make sure the game window is open with captcha visible"
Write-Host "2. Take a screenshot (Win+Shift+S) of JUST the game window"
Write-Host "3. Open the screenshot in Paint"
Write-Host "4. Hover mouse over the TOP-LEFT corner of WHITE captcha box"
Write-Host "5. Note the coordinates shown at bottom-left of Paint"
Write-Host "6. Hover mouse over BOTTOM-RIGHT corner of WHITE captcha box"
Write-Host "7. Note those coordinates too"
Write-Host ""
Write-Host "Enter the coordinates you found:" -ForegroundColor Cyan
Write-Host ""

$topLeftX = Read-Host "Top-Left X coordinate"
$topLeftY = Read-Host "Top-Left Y coordinate"
$bottomRightX = Read-Host "Bottom-Right X coordinate"
$bottomRightY = Read-Host "Bottom-Right Y coordinate"

$width = [int]$bottomRightX - [int]$topLeftX
$height = [int]$bottomRightY - [int]$topLeftY

Write-Host ""
Write-Host "Calculated captcha area:" -ForegroundColor Green
Write-Host "X: $topLeftX" -ForegroundColor Yellow
Write-Host "Y: $topLeftY" -ForegroundColor Yellow
Write-Host "Width: $width" -ForegroundColor Yellow
Write-Host "Height: $height" -ForegroundColor Yellow

$json = @"
{
  "GameWindowTitle": "Duke Client - By iamDuke",
  "CaptchaArea": {
    "X": $topLeftX,
    "Y": $topLeftY,
    "Width": $width,
    "Height": $height
  },
  "InputFieldPosition": {
    "X": $([int]$topLeftX + [int]($width/2) + 50),
    "Y": $([int]$topLeftY + 115)
  },
  "ConfirmButtonPosition": {
    "X": $([int]$topLeftX + [int]($width/2)),
    "Y": $([int]$topLeftY + 260)
  },
  "OCRSettings": {
    "TessdataPath": "./tessdata",
    "Language": "eng",
    "CharWhitelist": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
  },
  "AutomationSettings": {
    "DelayBetweenAttempts": 5000,
    "DelayAfterInput": 1000,
    "DelayAfterClick": 500,
    "MaxRetries": 3
  }
}
"@

Write-Host ""
Write-Host "New config.json content:" -ForegroundColor Cyan
Write-Host $json

$save = Read-Host "Do you want to save this to config.json? (Y/N)"
if ($save -eq "Y" -or $save -eq "y") {
    $json | Out-File -FilePath ".\config.json" -Encoding UTF8
    Write-Host "Config saved successfully!" -ForegroundColor Green
} else {
    Write-Host "Config not saved. Copy the JSON above and save manually." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
