@echo off
echo ========================================
echo    TEST OCR API - PowerShell Direct
echo ========================================
echo.

cd /d "%~dp0"

echo [1/3] Testing OCR API with PowerShell...
echo.

powershell -Command "
# Test OCR API trực tiếp
$apiUrl = 'https://api.ocr.space/parse/image'
$apiKey = 'K87601025288957'

# Tạo ảnh test đơn giản
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap(200, 50)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([System.Drawing.Color]::White)
$font = New-Object System.Drawing.Font('Arial', 20, [System.Drawing.FontStyle]::Bold)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::Black)
$graphics.DrawString('TEST', $font, $brush, 10, 10)
$graphics.Dispose()

# Lưu ảnh vào memory stream
$memoryStream = New-Object System.IO.MemoryStream
$bitmap.Save($memoryStream, [System.Drawing.Imaging.ImageFormat]::Png)
$imageBytes = $memoryStream.ToArray()
$memoryStream.Close()
$bitmap.Dispose()

Write-Host '✅ Đã tạo ảnh test: ' $imageBytes.Length ' bytes'

# Tạo form data
$boundary = [System.Guid]::NewGuid().ToString()
$LF = \"`r`n\"
$bodyLines = @()
$bodyLines += \"--$boundary\"
$bodyLines += \"Content-Disposition: form-data; name=`\"file`\"; filename=`\"test.png`\"\"
$bodyLines += \"Content-Type: image/png\"
$bodyLines += \"\"
$bodyLines += [System.Text.Encoding]::GetEncoding('iso-8859-1').GetString($imageBytes)
$bodyLines += \"--$boundary\"
$bodyLines += \"Content-Disposition: form-data; name=`\"apikey`\"\"
$bodyLines += \"\"
$bodyLines += $apiKey
$bodyLines += \"--$boundary\"
$bodyLines += \"Content-Disposition: form-data; name=`\"language`\"\"
$bodyLines += \"\"
$bodyLines += \"eng\"
$bodyLines += \"--$boundary\"
$bodyLines += \"Content-Disposition: form-data; name=`\"isOverlayRequired`\"\"
$bodyLines += \"\"
$bodyLines += \"true\"
$bodyLines += \"--$boundary--\"

$body = $bodyLines -join $LF
$bodyBytes = [System.Text.Encoding]::GetEncoding('iso-8859-1').GetBytes($body)

Write-Host '🔄 Gửi request đến OCR API...'

# Gửi request
try {
    $request = [System.Net.WebRequest]::Create($apiUrl)
    $request.Method = 'POST'
    $request.ContentType = \"multipart/form-data; boundary=$boundary\"
    $request.ContentLength = $bodyBytes.Length
    
    $requestStream = $request.GetRequestStream()
    $requestStream.Write($bodyBytes, 0, $bodyBytes.Length)
    $requestStream.Close()
    
    $response = $request.GetResponse()
    $responseStream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($responseStream)
    $responseText = $reader.ReadToEnd()
    $reader.Close()
    $responseStream.Close()
    $response.Close()
    
    Write-Host '✅ Response received:'
    Write-Host $responseText
    
    # Parse JSON response
    $json = $responseText | ConvertFrom-Json
    if ($json.IsErroredOnProcessing -eq $true) {
        Write-Host '❌ OCR API processing error'
    } else {
        if ($json.ParsedResults -and $json.ParsedResults.Count -gt 0) {
            $result = $json.ParsedResults[0].ParsedText
            Write-Host \"📝 OCR Result: '$result'\"
            if ($result -and $result.Trim() -ne '') {
                Write-Host '✅ OCR API hoạt động bình thường'
            } else {
                Write-Host '❌ OCR API trả về kết quả rỗng'
            }
        } else {
            Write-Host '❌ Không có kết quả OCR'
        }
    }
} catch {
    Write-Host \"❌ Lỗi: $($_.Exception.Message)\"
}

Write-Host '🎉 Test hoàn thành'
"

echo.
echo [2/3] PowerShell test completed
echo.

echo [3/3] Press any key to exit...
pause >nul
