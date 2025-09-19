using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using langla_duky.Models;

namespace langla_duky
{
    /// <summary>
    /// Quick OCR Test - Test OCR đơn giản nhất
    /// </summary>
    public class QuickOCRTest
    {
        public static void RunQuickTest()
        {
            Console.WriteLine("🔍 QUICK OCR TEST");
            Console.WriteLine("=================");

            try
            {
                Console.WriteLine("📁 Tìm hình ảnh test...");
                
                // Tìm hình ảnh
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
                        Console.WriteLine($"✅ Tìm thấy: {path}");
                        break;
                    }
                }
                
                if (imagePath == null)
                {
                    Console.WriteLine("❌ Không tìm thấy hình ảnh test!");
                    return;
                }

                // Load hình ảnh và thực hiện OCR
                Console.WriteLine($"📷 Đọc hình ảnh: {imagePath}");
                using (var image = new Bitmap(imagePath))
                {
                    Console.WriteLine($"📏 Kích thước: {image.Width}x{image.Height}");
                    
                    // Tìm tessdata
                    string[] tessdataPaths = {
                        "tessdata",
                        "../../../tessdata",
                        "../../tessdata"
                    };
                    
                    string? tessdataPath = null;
                    foreach (var path in tessdataPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            tessdataPath = path;
                            Console.WriteLine($"✅ Tìm thấy tessdata: {path}");
                            break;
                        }
                    }
                    
                    if (tessdataPath == null)
                    {
                        Console.WriteLine("❌ Không tìm thấy tessdata!");
                        return;
                    }
                    
                    // Thực hiện OCR đơn giản
                    Console.WriteLine("🔤 Đang đọc text từ hình ảnh...");
                    Console.WriteLine("🔧 Khởi tạo TesseractCaptchaReader...");
                    
                    try
                    {
                        Console.WriteLine("🔧 Đang thử OCR đơn giản...");
                        
                        // Thử OCR trực tiếp không dùng Task để tránh crash
                        Console.WriteLine("🔤 Khởi tạo TesseractEngine...");
                        using (var engine = new Tesseract.TesseractEngine(tessdataPath, "eng", Tesseract.EngineMode.TesseractAndLstm))
                        {
                            Console.WriteLine("✅ TesseractEngine đã khởi tạo thành công");
                            
                            // Cấu hình cơ bản
                            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
                            engine.SetVariable("tessedit_pageseg_mode", "8");
                            
                            Console.WriteLine("🔧 Đang chuyển đổi ảnh sang Pix...");
                            
                            // Chuyển đổi ảnh sang Pix
                            using (var pix = langla_duky.Models.PixConverter.ToPix(image))
                            {
                                Console.WriteLine("✅ Đã chuyển đổi ảnh thành công");
                                Console.WriteLine("🔍 Bắt đầu OCR...");
                                
                                using (var page = engine.Process(pix))
                                {
                                    string recognizedText = page.GetText()?.Trim() ?? "";
                                    Console.WriteLine($"📝 OCR hoàn thành: \"{recognizedText}\"");
                                    
                                    Console.WriteLine();
                                    Console.WriteLine("📝 KẾT QUẢ OCR:");
                                    Console.WriteLine("===============");
                                    Console.WriteLine($"Text đọc được: \"{recognizedText}\"");
                                    Console.WriteLine("===============");
                                    
                                    if (!string.IsNullOrEmpty(recognizedText))
                                    {
                                        Console.WriteLine("✅ Thành công! Đã đọc được text từ hình ảnh.");
                                    }
                                    else
                                    {
                                        Console.WriteLine("❌ Không đọc được text nào từ hình ảnh.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ocrEx)
                    {
                        Console.WriteLine($"❌ Lỗi trong quá trình OCR: {ocrEx.Message}");
                        Console.WriteLine($"📋 Chi tiết lỗi: {ocrEx.StackTrace}");
                        throw; // Re-throw để MainForm có thể catch
                    }
                }
                
                Console.WriteLine("✅ Test hoàn thành!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi: {ex.Message}");
                Console.WriteLine($"📋 Chi tiết: {ex.StackTrace}");
            }
        }
        
    }
}