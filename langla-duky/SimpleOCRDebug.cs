using System;
using System.Drawing;
using System.IO;

namespace langla_duky
{
    /// <summary>
    /// Simple OCR Debug - Test OCR cơ bản nhất
    /// </summary>
    public class SimpleOCRDebug
    {
        public static void RunSimpleDebug()
        {
            Console.WriteLine("🔍 SIMPLE OCR DEBUG");
            Console.WriteLine("===================");
            Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine();

            try
            {
                // Tìm hình ảnh
                string[] possibleImagePaths = {
                    "test_image/ocr-1.png",
                    "../../../test_image/ocr-1.png",
                    "../../test_image/ocr-1.png",
                    Path.Combine(Directory.GetCurrentDirectory(), "test_image", "ocr-1.png")
                };
                
                string? imagePath = null;
                foreach (var path in possibleImagePaths)
                {
                    Console.WriteLine($"🔍 Checking: {path}");
                    if (File.Exists(path))
                    {
                        imagePath = path;
                        Console.WriteLine($"✅ Found: {path}");
                        break;
                    }
                }
                
                if (imagePath == null)
                {
                    Console.WriteLine("❌ Image not found!");
                    return;
                }
                
                Console.WriteLine($"📷 Loading image: {imagePath}");
                
                // Load hình ảnh
                using (var image = new Bitmap(imagePath))
                {
                    Console.WriteLine($"📏 Image size: {image.Width}x{image.Height}");
                    
                    // Tạo thư mục debug
                    string debugFolder = "simple_debug";
                    if (!Directory.Exists(debugFolder))
                    {
                        Directory.CreateDirectory(debugFolder);
                        Console.WriteLine($"📁 Created: {Path.GetFullPath(debugFolder)}");
                    }
                    
                    // Lưu hình ảnh gốc
                    string originalPath = Path.Combine(debugFolder, "original.png");
                    image.Save(originalPath);
                    Console.WriteLine($"💾 Saved original: {originalPath}");
                    
                    // Test grayscale
                    Console.WriteLine("🔧 Converting to grayscale...");
                    using (var grayImage = ConvertToGrayscale(image))
                    {
                        string grayPath = Path.Combine(debugFolder, "grayscale.png");
                        grayImage.Save(grayPath);
                        Console.WriteLine($"💾 Saved grayscale: {grayPath}");
                        
                        // Test basic image info
                        Console.WriteLine("📊 Image Analysis:");
                        Console.WriteLine($"   - Width: {grayImage.Width}");
                        Console.WriteLine($"   - Height: {grayImage.Height}");
                        Console.WriteLine($"   - Pixel format: {grayImage.PixelFormat}");
                        
                        // Sample some pixels
                        Console.WriteLine("🔍 Sampling pixels:");
                        for (int i = 0; i < Math.Min(10, grayImage.Width); i += 2)
                        {
                            var pixel = grayImage.GetPixel(i, grayImage.Height / 2);
                            Console.WriteLine($"   - Pixel ({i}, {grayImage.Height / 2}): R={pixel.R}, G={pixel.G}, B={pixel.B}");
                        }
                    }
                    
                    Console.WriteLine("✅ Simple debug completed!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"📋 Stack: {ex.StackTrace}");
            }
        }
        
        static Bitmap ConvertToGrayscale(Bitmap original)
        {
            var gray = new Bitmap(original.Width, original.Height);
            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    var pixel = original.GetPixel(x, y);
                    int grayValue = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    gray.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }
            return gray;
        }
    }
}
