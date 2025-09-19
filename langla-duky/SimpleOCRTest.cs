using System;
using System.Drawing;
using System.IO;
using langla_duky.Models;

namespace langla_duky
{
    /// <summary>
    /// Simple OCR Test - Test OCR đơn giản với debug chi tiết
    /// </summary>
    public class SimpleOCRTest
    {
        public static void RunSimpleTest()
        {
            Console.WriteLine("🔍 SIMPLE OCR TEST");
            Console.WriteLine("==================");
            Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine();

            try
            {
                // Tìm hình ảnh
                string imagePath = "test_image/ocr-1.png";
                Console.WriteLine($"🔍 Checking: {imagePath}");
                
                if (!File.Exists(imagePath))
                {
                    Console.WriteLine("❌ Image not found!");
                    return;
                }
                
                Console.WriteLine($"✅ Found: {imagePath}");

                // Tạo thư mục debug
                string debugFolder = "ocr_debug_output";
                if (!Directory.Exists(debugFolder))
                {
                    Directory.CreateDirectory(debugFolder);
                    Console.WriteLine($"📁 Created: {Path.GetFullPath(debugFolder)}");
                }

                // Load hình ảnh
                Console.WriteLine($"📷 Loading image: {imagePath}");
                using (var image = new Bitmap(imagePath))
                {
                    Console.WriteLine($"📏 Image size: {image.Width}x{image.Height}");
                    
                    // Lưu hình ảnh gốc
                    string originalPath = Path.Combine(debugFolder, "simple_original.png");
                    image.Save(originalPath);
                    Console.WriteLine($"💾 Saved original: {originalPath}");
                    
                    // Test OCR trực tiếp
                    Console.WriteLine("🔤 Testing OCR directly...");
                    TestOCRDirectly(image, debugFolder);
                }
                
                Console.WriteLine("✅ Simple test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"📋 Stack: {ex.StackTrace}");
            }
        }
        
        private static void TestOCRDirectly(Bitmap image, string debugFolder)
        {
            try
            {
                Console.WriteLine("🔤 Initializing Tesseract OCR engine...");
                
                // Test với TesseractCaptchaReader
                using (var reader = new TesseractCaptchaReader("tessdata", "eng"))
                {
                    Console.WriteLine("✅ Tesseract engine initialized");
                    
                    // Thực hiện OCR
                    Console.WriteLine("🔍 Reading text from image...");
                    string recognizedText = reader.ReadCaptcha(image);
                    
                    // Hiển thị kết quả
                    Console.WriteLine();
                    Console.WriteLine("📝 OCR RESULT 📝");
                    Console.WriteLine("================");
                    if (!string.IsNullOrEmpty(recognizedText))
                    {
                        Console.WriteLine($"✅ Recognized text: \"{recognizedText}\"");
                        
                        // Lưu kết quả vào file
                        string resultPath = Path.Combine(debugFolder, "simple_ocr_result.txt");
                        File.WriteAllText(resultPath, recognizedText);
                        Console.WriteLine($"💾 OCR result saved to: {resultPath}");
                    }
                    else
                    {
                        Console.WriteLine("❌ No text recognized or OCR failed");
                        
                        // Lưu thông tin debug
                        string debugPath = Path.Combine(debugFolder, "simple_debug.txt");
                        string debugInfo = $"OCR failed to recognize any text\n" +
                                          $"Image size: {image.Width}x{image.Height}\n" +
                                          $"Timestamp: {DateTime.Now}\n" +
                                          $"TessData path: tessdata\n" +
                                          $"Language: eng";
                        File.WriteAllText(debugPath, debugInfo);
                        Console.WriteLine($"💾 Debug info saved to: {debugPath}");
                    }
                    Console.WriteLine("================");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OCR Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner error: {ex.InnerException.Message}");
                }
                Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
                
                // Lưu error info
                string errorPath = Path.Combine(debugFolder, "simple_error.txt");
                string errorInfo = $"OCR Error: {ex.Message}\n" +
                                  $"Inner Error: {ex.InnerException?.Message}\n" +
                                  $"Stack Trace: {ex.StackTrace}\n" +
                                  $"Timestamp: {DateTime.Now}";
                File.WriteAllText(errorPath, errorInfo);
                Console.WriteLine($"💾 Error info saved to: {errorPath}");
            }
        }
    }
}
