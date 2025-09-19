using System;
using System.Drawing;
using System.IO;
using langla_duky.Models;

namespace langla_duky
{
    /// <summary>
    /// Direct OCR Test - Test trực tiếp với hình ảnh ocr-1.png
    /// </summary>
    public class DirectOCRTest
    {
        public static void RunDirectTest()
        {
            Console.WriteLine("🔍 DIRECT OCR TEST");
            Console.WriteLine("==================");
            Console.WriteLine("Test OCR trực tiếp với hình ảnh ocr-1.png");
            Console.WriteLine();

            try
            {
                // Đường dẫn hình ảnh - tìm từ nhiều vị trí có thể
                string[] possibleImagePaths = {
                    "test_image/ocr-1.png",  // Từ project root
                    "../../../test_image/ocr-1.png",  // Từ bin/Debug/net8.0-windows
                    "../../test_image/ocr-1.png"  // Fallback
                };
                
                string? imagePath = null;
                foreach (var path in possibleImagePaths)
                {
                    if (File.Exists(path))
                    {
                        imagePath = path;
                        break;
                    }
                }
                
                if (imagePath == null)
                {
                    Console.WriteLine($"❌ Không tìm thấy hình ảnh test ở bất kỳ vị trí nào:");
                    foreach (var path in possibleImagePaths)
                    {
                        Console.WriteLine($"   - {path}");
                    }
                    return;
                }
                
                if (!File.Exists(imagePath))
                {
                    Console.WriteLine($"❌ Không tìm thấy hình ảnh: {imagePath}");
                    return;
                }

                Console.WriteLine($"✅ Tìm thấy hình ảnh: {imagePath}");
                
                // Load hình ảnh, sử dụng using để đảm bảo rằng hình ảnh được đóng sau khi sử dụng
                using (var image = new Bitmap(imagePath))
                {
                    Console.WriteLine($"📏 Kích thước hình ảnh: {image.Width}x{image.Height}");
                    
                    // Tạo thư mục debug output - sử dụng đường dẫn tương đối từ project root
                    string debugFolder = "ocr_debug_output";
                    if (!Directory.Exists(debugFolder))
                    {
                        Directory.CreateDirectory(debugFolder);
                        Console.WriteLine($"📁 Đã tạo thư mục: {Path.GetFullPath(debugFolder)}");
                    }
                    else
                    {
                        Console.WriteLine($"📁 Thư mục đã tồn tại: {Path.GetFullPath(debugFolder)}");
                    }
                    
                    // Khởi tạo OCR reader
                    Console.WriteLine("🔧 Khởi tạo Tesseract OCR...");
                    Console.WriteLine($"📁 Tessdata path: {Path.GetFullPath("./tessdata")}");
                    
                    // Kiểm tra tessdata trước
                    string[] possibleTessdataPaths = {
                        "./tessdata",
                        "../../../tessdata",
                        "../../tessdata"
                    };
                    
                    string? tessdataPath = null;
                    foreach (var path in possibleTessdataPaths)
                    {
                        Console.WriteLine($"🔍 Checking tessdata: {path}");
                        if (Directory.Exists(path))
                        {
                            tessdataPath = path;
                            Console.WriteLine($"✅ Found tessdata: {Path.GetFullPath(path)}");
                            break;
                        }
                    }
                    
                    if (tessdataPath == null)
                    {
                        Console.WriteLine("❌ Không tìm thấy tessdata!");
                        return;
                    }
                    
                    try
                    {
                        using (var ocrReader = new TesseractCaptchaReader(tessdataPath, "eng"))
                        {
                            Console.WriteLine("✅ OCR Reader đã sẵn sàng");
                        
                        // Test với hình ảnh gốc
                        Console.WriteLine("\n🔍 Testing với hình ảnh gốc...");
                        try
                        {
                            string originalResult = ocrReader.ReadCaptcha(image);
                            Console.WriteLine($"📝 Kết quả gốc: '{originalResult}'");
                            Console.WriteLine($"✅ Hợp lệ: {ocrReader.IsValidCaptcha(originalResult)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Lỗi OCR gốc: {ex.Message}");
                        }
                        
                        // Test với các phương pháp xử lý khác nhau
                        Console.WriteLine("\n🔧 Testing với các phương pháp xử lý...");
                        
                        TestMethod("Enhanced Preprocessing", () => ScreenCapture.PreprocessCaptchaImage(image), ocrReader, debugFolder);
                        TestMethod("Pink/Magenta Isolation", () => ScreenCapture.IsolatePinkMagentaText(image), ocrReader, debugFolder);
                        TestMethod("Grayscale + Threshold", () => {
                            var gray = ScreenCapture.ConvertToGrayscale(image);
                            var threshold = ScreenCapture.ApplyThreshold(gray, 150);
                            gray.Dispose();
                            return threshold;
                        }, ocrReader, debugFolder);
                        TestMethod("Scale 2x", () => ScreenCapture.ScaleImage(image, 2.0f), ocrReader, debugFolder);
                        TestMethod("Scale 3x", () => ScreenCapture.ScaleImage(image, 3.0f), ocrReader, debugFolder);
                        TestMethod("Scale 4x", () => ScreenCapture.ScaleImage(image, 4.0f), ocrReader, debugFolder);
                        TestMethod("Adaptive Threshold", () => {
                            var gray = ScreenCapture.ConvertToGrayscale(image);
                            var threshold = ScreenCapture.ApplyAdaptiveThreshold(gray);
                            gray.Dispose();
                            return threshold;
                        }, ocrReader, debugFolder);
                        
                        Console.WriteLine("\n📊 TỔNG KẾT:");
                        Console.WriteLine("=============");
                        Console.WriteLine($"🖼️ Hình ảnh: {Path.GetFileName(imagePath)}");
                        Console.WriteLine($"📏 Kích thước: {image.Width}x{image.Height}");
                        Console.WriteLine($"📁 Debug folder: {Path.GetFullPath(debugFolder)}");
                        
                        // Kiểm tra files đã tạo
                        var debugFiles = Directory.GetFiles(debugFolder);
                        Console.WriteLine($"📸 Files debug đã tạo: {debugFiles.Length}");
                        foreach (var file in debugFiles)
                        {
                            Console.WriteLine($"   - {Path.GetFileName(file)}");
                        }
                        }
                    }
                    catch (Exception ocrEx)
                    {
                        Console.WriteLine($"❌ Lỗi khởi tạo OCR: {ocrEx.Message}");
                        Console.WriteLine($"📋 Stack trace: {ocrEx.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi tổng thể: {ex.Message}");
                Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            }
        }
        
        static void TestMethod(string methodName, Func<Bitmap> preprocessingFunc, TesseractCaptchaReader ocrReader, string debugFolder)
        {
            try
            {
                Console.WriteLine($"🔧 Testing {methodName}...");
                
                using (var processedImage = preprocessingFunc())
                {
                    // Lưu hình ảnh đã xử lý để debug
                    string debugPath = Path.Combine(debugFolder, $"ocr-1_{methodName.Replace(" ", "_").Replace("/", "_")}.png");
                    
                    try
                    {
                        processedImage.Save(debugPath);
                        Console.WriteLine($"   💾 Saved: {debugPath}");
                    }
                    catch (Exception saveEx)
                    {
                        Console.WriteLine($"   ❌ Lỗi lưu file: {saveEx.Message}");
                        return;
                    }
                    
                    // Test OCR
                    try
                    {
                        string result = ocrReader.ReadCaptcha(processedImage);
                        bool isValid = ocrReader.IsValidCaptcha(result);
                        
                        Console.WriteLine($"   📝 Kết quả: '{result}'");
                        Console.WriteLine($"   ✅ Hợp lệ: {isValid}");
                    }
                    catch (Exception ocrEx)
                    {
                        Console.WriteLine($"   ❌ Lỗi OCR: {ocrEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Lỗi tổng thể: {ex.Message}");
                Console.WriteLine($"   📋 Stack trace: {ex.StackTrace}");
            }
        }
    }
}
