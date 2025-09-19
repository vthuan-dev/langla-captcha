using System;
using System.Drawing;
using System.IO;
using Tesseract;

namespace langla_duky
{
    public static class SimpleTesseractTest
    {
        public static void RunTest()
        {
            Console.WriteLine("🔍 Simple Tesseract Test");
            Console.WriteLine("========================");
            
            try
            {
                // Kiểm tra paths
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
                
                // Kiểm tra file eng.traineddata
                string engFile = Path.Combine(tessdataPath, "eng.traineddata");
                if (File.Exists(engFile))
                {
                    var fileInfo = new FileInfo(engFile);
                    Console.WriteLine($"✅ eng.traineddata: {fileInfo.Length} bytes");
                }
                else
                {
                    Console.WriteLine("❌ Không tìm thấy eng.traineddata!");
                    return;
                }
                
                // Thử khởi tạo Tesseract
                Console.WriteLine("🚀 Đang khởi tạo Tesseract...");
                
                using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
                {
                    Console.WriteLine("✅ Tesseract Engine khởi tạo thành công!");
                    
                    // Thử đọc một ảnh đơn giản
                    string[] possibleImagePaths = {
                        "test_image/ocr-1.png",
                        "../../../test_image/ocr-1.png",
                        "../../test_image/ocr-1.png"
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
                    
                    if (imagePath != null)
                    {
                        Console.WriteLine($"✅ Tìm thấy ảnh test: {imagePath}");
                        
                        using (var img = Pix.LoadFromFile(imagePath))
                        {
                            Console.WriteLine("✅ Đã load ảnh thành công");
                            
                            using (var page = engine.Process(img))
                            {
                                string text = page.GetText();
                                Console.WriteLine($"✅ OCR Result: '{text.Trim()}'");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Không tìm thấy ảnh test");
                    }
                }
                
                Console.WriteLine("✅ Test hoàn thành thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi: {ex.Message}");
                Console.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            }
        }
    }
}

