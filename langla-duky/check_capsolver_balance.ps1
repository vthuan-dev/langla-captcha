$apiKey = "CAP-C90A0F23AE0DC0C4FDEFE047059EBBF134117DB9A0A8826E9DF5752BCFFF2507"
$url = "https://api.capsolver.com/getBalance"

$payload = @{
    clientKey = $apiKey
} | ConvertTo-Json

Write-Host "Checking CapSolver balance..."
try {
    $response = Invoke-RestMethod -Uri $url -Method Post -Body $payload -ContentType "application/json"
    
    if ($response.errorId -eq 0) {
        Write-Host "✅ Balance: $($response.balance)" -ForegroundColor Green
    } else {
        Write-Host "❌ Error: $($response.errorCode) - $($response.errorDescription)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Connection error: $_" -ForegroundColor Red
}

