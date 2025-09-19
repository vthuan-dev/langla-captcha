using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using langla_duky.Models;

namespace langla_duky
{
    /// <summary>
    /// Tool debug để capture ảnh full window và đo coordinates chính xác cho captcha
    /// </summary>
    public class CaptchaCoordinateDebugger
    {
        public static void RunDebug()
        {
            try
            {
                Console.WriteLine("🔍 CAPTCHA COORDINATE DEBUGGER");
                Console.WriteLine("==============================");
                Console.WriteLine("Tool này sẽ capture full window và lưu ảnh để đo coordinates");
                Console.WriteLine();

                // Tìm game window
                Console.WriteLine("🎯 Tìm game window...");
                var gameWindow = new GameWindow("Duke Client - By iamDuke");
                
                if (!gameWindow.FindGameWindowWithMultipleInstances())
                {
                    Console.WriteLine("❌ Không tìm thấy game window!");
                    return;
                }

                Console.WriteLine($"✅ Tìm thấy game window: {gameWindow.WindowTitle}");
                Console.WriteLine($"📏 Window bounds: {gameWindow.Bounds}");

                // Update window info
                gameWindow.UpdateWindowInfo();
                var bounds = gameWindow.Bounds;
                Console.WriteLine($"📐 Updated bounds: Width={bounds.Width}, Height={bounds.Height}");

                // Capture full window
                Console.WriteLine();
                Console.WriteLine("📸 Capturing full game window...");
                
                using (var fullWindowImage = ScreenCapture.CaptureWindowClientArea(gameWindow.Handle, 
                    new Rectangle(0, 0, bounds.Width, bounds.Height)))
                {
                    if (fullWindowImage == null)
                    {
                        Console.WriteLine("❌ Không thể capture game window!");
                        return;
                    }

                    Console.WriteLine($"✅ Captured image: {fullWindowImage.Width}x{fullWindowImage.Height}");

                    // Tạo debug folder
                    string debugFolder = "coordinate_debug";
                    Directory.CreateDirectory(debugFolder);
                    
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    
                    // Lưu ảnh full window với grid overlay
                    string fullWindowPath = Path.Combine(debugFolder, $"full_window_with_grid_{timestamp}.png");
                    using (var gridImage = AddCoordinateGrid(fullWindowImage))
                    {
                        gridImage.Save(fullWindowPath);
                        Console.WriteLine($"💾 Saved full window with grid: {fullWindowPath}");
                    }

                    // Lưu ảnh full window gốc
                    string originalPath = Path.Combine(debugFolder, $"full_window_original_{timestamp}.png");
                    fullWindowImage.Save(originalPath);
                    Console.WriteLine($"💾 Saved original full window: {originalPath}");

                    // Test capture với coordinates hiện tại
                    Console.WriteLine();
                    Console.WriteLine("🔍 Testing current coordinates...");
                    var currentConfig = Config.LoadFromFile();
                    var captchaArea = currentConfig.CaptchaArea;
                    Console.WriteLine($"📍 Current captcha area: X={captchaArea.X}, Y={captchaArea.Y}, W={captchaArea.Width}, H={captchaArea.Height}");

                    // Crop vùng captcha với coordinates hiện tại
                    if (captchaArea.X + captchaArea.Width <= fullWindowImage.Width && 
                        captchaArea.Y + captchaArea.Height <= fullWindowImage.Height)
                    {
                        using (var captchaCrop = new Bitmap(captchaArea.Width, captchaArea.Height))
                        {
                            using (var g = Graphics.FromImage(captchaCrop))
                            {
                                g.DrawImage(fullWindowImage, 
                                    new Rectangle(0, 0, captchaArea.Width, captchaArea.Height), 
                                    captchaArea, GraphicsUnit.Pixel);
                            }

                            string captchaPath = Path.Combine(debugFolder, $"current_captcha_crop_{timestamp}.png");
                            captchaCrop.Save(captchaPath);
                            Console.WriteLine($"💾 Saved current captcha crop: {captchaPath}");
                            
                            // Analyze captcha area
                            AnalyzeCaptchaArea(captchaCrop, captchaArea);
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Current coordinates are outside window bounds!");
                    }

                    // Suggest alternative coordinates
                    Console.WriteLine();
                    Console.WriteLine("🎯 COORDINATE SUGGESTIONS:");
                    Console.WriteLine("==========================");
                    
                    // Test multiple areas around the center
                    var suggestions = new[]
                    {
                        new Rectangle(500, 320, 200, 60),  // Slightly left, lower
                        new Rectangle(520, 330, 200, 60),  // Center-left, lower
                        new Rectangle(540, 340, 200, 60),  // Current X, lower Y
                        new Rectangle(560, 330, 200, 60),  // Slightly right, lower
                        new Rectangle(540, 350, 200, 60),  // Current X, even lower
                    };

                    for (int i = 0; i < suggestions.Length; i++)
                    {
                        var area = suggestions[i];
                        Console.WriteLine($"Suggestion {i + 1}: X={area.X}, Y={area.Y}, W={area.Width}, H={area.Height}");
                        
                        if (area.X + area.Width <= fullWindowImage.Width && 
                            area.Y + area.Height <= fullWindowImage.Height)
                        {
                            using (var testCrop = new Bitmap(area.Width, area.Height))
                            {
                                using (var g = Graphics.FromImage(testCrop))
                                {
                                    g.DrawImage(fullWindowImage, 
                                        new Rectangle(0, 0, area.Width, area.Height), 
                                        area, GraphicsUnit.Pixel);
                                }

                                string testPath = Path.Combine(debugFolder, $"suggestion_{i + 1}_{timestamp}.png");
                                testCrop.Save(testPath);
                                Console.WriteLine($"   💾 Saved: {testPath}");
                            }
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("🎉 DEBUG HOÀN THÀNH!");
                    Console.WriteLine($"📁 Kiểm tra folder: {Path.GetFullPath(debugFolder)}");
                    Console.WriteLine("📋 Hướng dẫn:");
                    Console.WriteLine("   1. Mở file 'full_window_with_grid_*.png' để xem toàn bộ cửa sổ");
                    Console.WriteLine("   2. Tìm vùng captcha 'v o l v' trong ảnh");
                    Console.WriteLine("   3. Đo coordinates bằng grid (mỗi ô = 50 pixels)");
                    Console.WriteLine("   4. So sánh với các suggestion_*.png để chọn đúng");
                    Console.WriteLine("   5. Cập nhật coordinates trong Config.cs");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi debug: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static Bitmap AddCoordinateGrid(Bitmap originalImage)
        {
            var gridImage = new Bitmap(originalImage);
            
            using (var g = Graphics.FromImage(gridImage))
            {
                // Vẽ grid với khoảng cách 50 pixels
                var gridPen = new Pen(Color.Red, 1);
                var textBrush = new SolidBrush(Color.Red);
                var font = new Font("Arial", 10, FontStyle.Bold);

                // Vertical lines
                for (int x = 50; x < originalImage.Width; x += 50)
                {
                    g.DrawLine(gridPen, x, 0, x, originalImage.Height);
                    g.DrawString(x.ToString(), font, textBrush, x + 2, 5);
                }

                // Horizontal lines  
                for (int y = 50; y < originalImage.Height; y += 50)
                {
                    g.DrawLine(gridPen, 0, y, originalImage.Width, y);
                    g.DrawString(y.ToString(), font, textBrush, 5, y + 2);
                }

                // Highlight current captcha area
                var currentConfig = Config.LoadFromFile();
                var captchaArea = currentConfig.CaptchaArea;
                var highlightPen = new Pen(Color.Lime, 3);
                g.DrawRectangle(highlightPen, captchaArea);
                
                // Label current area
                string label = $"Current: {captchaArea.X},{captchaArea.Y}";
                g.DrawString(label, font, new SolidBrush(Color.Lime), captchaArea.X, captchaArea.Y - 20);
            }

            return gridImage;
        }

        private static void AnalyzeCaptchaArea(Bitmap captchaImage, Rectangle area)
        {
            Console.WriteLine();
            Console.WriteLine("🔍 ANALYZING CAPTCHA AREA:");
            Console.WriteLine($"📍 Area: X={area.X}, Y={area.Y}, W={area.Width}, H={area.Height}");
            Console.WriteLine($"📏 Image: {captchaImage.Width}x{captchaImage.Height}");

            // Color analysis
            int totalPixels = captchaImage.Width * captchaImage.Height;
            int whitePixels = 0;
            int coloredPixels = 0;
            int darkPixels = 0;

            for (int x = 0; x < captchaImage.Width; x += 2)
            {
                for (int y = 0; y < captchaImage.Height; y += 2)
                {
                    Color pixel = captchaImage.GetPixel(x, y);
                    
                    if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
                        whitePixels++;
                    else if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
                        darkPixels++;
                    else
                        coloredPixels++;
                }
            }

            int samples = (captchaImage.Width / 2) * (captchaImage.Height / 2);
            Console.WriteLine($"🎨 Color analysis (sampled {samples} pixels):");
            Console.WriteLine($"   White: {(double)whitePixels / samples:P1}");
            Console.WriteLine($"   Colored: {(double)coloredPixels / samples:P1}");
            Console.WriteLine($"   Dark: {(double)darkPixels / samples:P1}");

            // Determine if this looks like a captcha area
            double coloredRatio = (double)coloredPixels / samples;
            bool likelyCaptcha = coloredRatio > 0.1 && coloredRatio < 0.8;
            
            Console.WriteLine($"🎯 Likely contains captcha: {likelyCaptcha}");
            if (!likelyCaptcha)
            {
                if (coloredRatio <= 0.1)
                    Console.WriteLine("   ❌ Too few colored pixels - might be empty area");
                else
                    Console.WriteLine("   ❌ Too many colored pixels - might be background");
            }
        }
    }
}
