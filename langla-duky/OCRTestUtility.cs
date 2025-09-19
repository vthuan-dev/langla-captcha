using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using langla_duky.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace langla_duky
{
    /// <summary>
    /// Simple utility to test OCR functionality independently
    /// </summary>
    public class OCRTestUtility
    {
        public static async Task TestOCRWithLatestCaptcha()
        {
            try
            {
                Console.WriteLine("=== OCR Test Utility ===");
                
                // Find the latest captcha debug image
                string debugFolder = "bin\\Debug\\net8.0-windows\\captcha_debug";
                if (!Directory.Exists(debugFolder))
                {
                    Console.WriteLine($"❌ Debug folder not found: {debugFolder}");
                    return;
                }
                
                var captchaFiles = Directory.GetFiles(debugFolder, "captcha_area_*.png");
                if (captchaFiles.Length == 0)
                {
                    Console.WriteLine("❌ No captcha debug images found");
                    return;
                }
                
                // Get the most recent file
                string latestFile = "";
                DateTime latestTime = DateTime.MinValue;
                
                foreach (string file in captchaFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime > latestTime)
                    {
                        latestTime = fileInfo.LastWriteTime;
                        latestFile = file;
                    }
                }
                
                if (string.IsNullOrEmpty(latestFile))
                {
                    Console.WriteLine("❌ Could not determine latest captcha file");
                    return;
                }
                
                Console.WriteLine($"📸 Testing with latest captcha: {Path.GetFileName(latestFile)}");
                Console.WriteLine($"📅 File date: {latestTime:yyyy-MM-dd HH:mm:ss}");
                
                // Load and test the image
                using (Bitmap captchaImage = new Bitmap(latestFile))
                {
                    Console.WriteLine($"🖼️ Image size: {captchaImage.Width}x{captchaImage.Height}");
                    
                    // Tesseract OCR (OpenCV binarize) chạy nền
                    Console.WriteLine("\n--- Testing Tesseract OCR (local) ---");
                    await Task.Run(() =>
                    {
                        using (var ms = new MemoryStream())
                        {
                            captchaImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            var mat = Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
                            using var gray = new Mat();
                            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                            using var blur = new Mat();
                            Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(3, 3), 0);
                            using var bin = new Mat();
                            Cv2.Threshold(blur, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                            using var binBmp = bin.ToBitmap();
                            using var ms2 = new MemoryStream();
                            binBmp.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                            using var pix = Pix.LoadFromMemory(ms2.ToArray());
                            using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
                            engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                            using var page = engine.Process(pix);
                            string tesseractResult = page.GetText().Trim();
                            Console.WriteLine($"✅ Tesseract (local) result: '{tesseractResult}' (Length: {tesseractResult?.Length ?? 0})");
                        }
                    });
                    
                    // Test image analysis
                    Console.WriteLine("\n--- Image Analysis ---");
                    TestImageAnalysis(captchaImage);
                }
                
                Console.WriteLine("\n=== OCR Test Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OCR Test failed: {ex.Message}");
            }
        }
        
        private static void TestImageAnalysis(Bitmap image)
        {
            try
            {
                // Check if image is blank
                int totalPixels = 0;
                int blankPixels = 0;
                int coloredPixels = 0;
                
                Dictionary<string, int> colorCounts = new Dictionary<string, int>();
                
                for (int x = 0; x < image.Width; x += 2)
                {
                    for (int y = 0; y < image.Height; y += 2)
                    {
                        totalPixels++;
                        Color pixel = image.GetPixel(x, y);
                        
                        // Check if blank/white
                        if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
                        {
                            blankPixels++;
                        }
                        else
                        {
                            coloredPixels++;
                            
                            // Analyze colors
                            if (pixel.R > 120 && pixel.R < 200 && pixel.G > 60 && pixel.G < 140 && pixel.B < 80)
                                colorCounts["brown"] = colorCounts.GetValueOrDefault("brown", 0) + 1;
                            else if (pixel.R > 180 && pixel.G > 160 && pixel.B < 80)
                                colorCounts["yellow"] = colorCounts.GetValueOrDefault("yellow", 0) + 1;
                            else if (pixel.R > 100 && pixel.R < 170 && pixel.G < 100 && pixel.B > 100)
                                colorCounts["purple"] = colorCounts.GetValueOrDefault("purple", 0) + 1;
                            else if (pixel.R < 80 && pixel.G > 140 && pixel.B > 100)
                                colorCounts["green"] = colorCounts.GetValueOrDefault("green", 0) + 1;
                            else
                                colorCounts["other"] = colorCounts.GetValueOrDefault("other", 0) + 1;
                        }
                    }
                }
                
                double blankRatio = (double)blankPixels / totalPixels;
                double coloredRatio = (double)coloredPixels / totalPixels;
                
                Console.WriteLine($"📊 Total pixels sampled: {totalPixels}");
                Console.WriteLine($"⚪ Blank pixels: {blankPixels} ({blankRatio:P1})");
                Console.WriteLine($"🎨 Colored pixels: {coloredPixels} ({coloredRatio:P1})");
                Console.WriteLine($"🌈 Color breakdown: {string.Join(", ", colorCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                Console.WriteLine($"📝 Is mostly blank: {(blankRatio > 0.9 ? "YES" : "NO")}");
                Console.WriteLine($"🎯 Has captcha colors: {(colorCounts.Count > 1 && coloredRatio > 0.1 ? "YES" : "NO")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image analysis failed: {ex.Message}");
            }
        }
        
        public static void RunQuickTest()
        {
            Console.WriteLine("=== Quick OCR Test ===");
            
            // Create a simple test image with text
            using (Bitmap testImage = new Bitmap(200, 50))
            {
                using (Graphics g = Graphics.FromImage(testImage))
                {
                    g.Clear(Color.White);
                    g.DrawString("TEST123", new Font("Arial", 16, FontStyle.Bold), Brushes.Black, 10, 10);
                }
                
                // Save test image
                string testPath = "test_captcha.png";
                testImage.Save(testPath);
                Console.WriteLine($"📸 Created test image: {testPath}");
                
                // Test OCR on simple image
                try
                {
                    var tesseractReader = new TesseractCaptchaReader();
                    string result = tesseractReader.ReadCaptcha(testImage);
                    Console.WriteLine($"✅ Test result: '{result}'");
                    Console.WriteLine($"✅ Is valid: {tesseractReader.IsValidCaptcha(result)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Test failed: {ex.Message}");
                }
            }
        }
    }
}
