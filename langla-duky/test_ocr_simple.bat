@echo off
echo ===============================
echo Simple OCR Test
echo ===============================

echo.
echo This will test if OCR is working at all by:
echo 1. Creating a simple test image with text "TEST123"
echo 2. Running OCR on it 
echo 3. Testing with your latest captcha debug images
echo.

echo Building project...
dotnet build -q

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

echo.
echo Creating simple test program...

echo using System; > OCRSimpleTest.cs
echo using System.Drawing; >> OCRSimpleTest.cs  
echo using System.Threading.Tasks; >> OCRSimpleTest.cs
echo using langla_duky.Models; >> OCRSimpleTest.cs
echo using langla_duky.Services; >> OCRSimpleTest.cs
echo. >> OCRSimpleTest.cs
echo public class SimpleOCRTest >> OCRSimpleTest.cs
echo { >> OCRSimpleTest.cs
echo     public static async Task Main() >> OCRSimpleTest.cs
echo     { >> OCRSimpleTest.cs
echo         Console.WriteLine("=== Simple OCR Test ==="); >> OCRSimpleTest.cs
echo. >> OCRSimpleTest.cs  
echo         // Test 1: Simple black text on white >> OCRSimpleTest.cs
echo         using (var testImage = new Bitmap(200, 50)) >> OCRSimpleTest.cs
echo         { >> OCRSimpleTest.cs
echo             using (var g = Graphics.FromImage(testImage)) >> OCRSimpleTest.cs
echo             { >> OCRSimpleTest.cs
echo                 g.Clear(Color.White); >> OCRSimpleTest.cs
echo                 g.DrawString("TEST123", new Font("Arial", 16, FontStyle.Bold), Brushes.Black, 10, 10); >> OCRSimpleTest.cs
echo             } >> OCRSimpleTest.cs
echo. >> OCRSimpleTest.cs
echo             testImage.Save("simple_test.png"); >> OCRSimpleTest.cs
echo             Console.WriteLine("📸 Created test image: simple_test.png"); >> OCRSimpleTest.cs
echo. >> OCRSimpleTest.cs
echo             try >> OCRSimpleTest.cs
echo             { >> OCRSimpleTest.cs
echo                 var reader = new TesseractCaptchaReader(); >> OCRSimpleTest.cs
echo                 string result = reader.ReadCaptcha(testImage); >> OCRSimpleTest.cs
echo                 Console.WriteLine($"✅ Tesseract result: '{result}'"); >> OCRSimpleTest.cs
echo                 Console.WriteLine($"✅ Is valid: {reader.IsValidCaptcha(result)}"); >> OCRSimpleTest.cs
echo             } >> OCRSimpleTest.cs
echo             catch (Exception ex) >> OCRSimpleTest.cs
echo             { >> OCRSimpleTest.cs
echo                 Console.WriteLine($"❌ Tesseract failed: {ex.Message}"); >> OCRSimpleTest.cs
echo             } >> OCRSimpleTest.cs
echo         } >> OCRSimpleTest.cs
echo. >> OCRSimpleTest.cs
echo         // Test 2: Test with latest debug image if exists >> OCRSimpleTest.cs
echo         await OCRTestUtility.TestOCRWithLatestCaptcha(); >> OCRSimpleTest.cs
echo. >> OCRSimpleTest.cs
echo         Console.WriteLine("\\n=== Test Complete ==="); >> OCRSimpleTest.cs
echo         Console.WriteLine("Press any key to exit..."); >> OCRSimpleTest.cs
echo         Console.ReadKey(); >> OCRSimpleTest.cs
echo     } >> OCRSimpleTest.cs
echo } >> OCRSimpleTest.cs

echo.
echo Running OCR test...
echo.

dotnet run --project . OCRSimpleTest.cs

echo.
echo Test completed. Check the results above.
echo.
echo If Tesseract fails on simple "TEST123", there's a Tesseract installation issue.
echo If it works on simple text but fails on captcha images, it's an image quality issue.
echo.
pause

del OCRSimpleTest.cs