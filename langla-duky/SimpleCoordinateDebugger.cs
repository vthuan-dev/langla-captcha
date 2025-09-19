using System;
using System.Drawing;
using System.IO;
using langla_duky.Models;

namespace langla_duky
{
    /// <summary>
    /// Simple debugger chỉ dùng OCR API để debug coordinates
    /// </summary>
    public class SimpleCoordinateDebugger
    {
        public static void RunSimpleDebug()
        {
            try
            {
                Console.WriteLine("🔍 SIMPLE COORDINATE DEBUGGER");
                Console.WriteLine("==============================");
                Console.WriteLine("Tool debug đơn giản chỉ dùng OCR API");
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
                gameWindow.UpdateWindowInfo();
                var bounds = gameWindow.Bounds;
                Console.WriteLine($"📏 Window bounds: {bounds.Width}x{bounds.Height}");

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

                    Console.WriteLine($"✅ Captured: {fullWindowImage.Width}x{fullWindowImage.Height}");

                    // Tạo debug folder
                    string debugFolder = "coordinate_debug";
                    Directory.CreateDirectory(debugFolder);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    
                    // Lưu ảnh full window với grid - DISABLED
                    string gridPath = Path.Combine(debugFolder, $"full_window_no_grid_{timestamp}.png");
                    using (var gridImage = fullWindowImage) // Use original image without grid
                    {
                        gridImage.Save(gridPath);
                        Console.WriteLine($"💾 Saved image without grid: {gridPath}");
                    }

                    // Lưu ảnh gốc
                    string originalPath = Path.Combine(debugFolder, $"full_window_original_{timestamp}.png");
                    fullWindowImage.Save(originalPath);
                    Console.WriteLine($"💾 Saved original: {originalPath}");

                    // Test với coordinates hiện tại
                    Console.WriteLine();
                    Console.WriteLine("🔍 Testing current coordinates...");
                    var config = new Config(); // Dùng default values
                    TestCoordinates(fullWindowImage, config.CaptchaArea, "current", debugFolder, timestamp);

                    // Test với một số coordinates suggestions
                    Console.WriteLine();
                    Console.WriteLine("🎯 Testing coordinate suggestions...");
                    
                    var suggestions = new[]
                    {
                        new Rectangle(520, 340, 200, 60),  // Slightly higher
                        new Rectangle(540, 350, 200, 60),  // Current X, lower Y
                        new Rectangle(540, 360, 200, 60),  // Even lower
                        new Rectangle(520, 350, 240, 70),  // Wider area
                        new Rectangle(500, 340, 280, 80),  // Much wider
                    };

                    for (int i = 0; i < suggestions.Length; i++)
                    {
                        TestCoordinates(fullWindowImage, suggestions[i], $"suggestion_{i + 1}", debugFolder, timestamp);
                    }

                    Console.WriteLine();
                    Console.WriteLine("🎉 DEBUG COMPLETED!");
                    Console.WriteLine($"📁 Check folder: {Path.GetFullPath(debugFolder)}");
                    Console.WriteLine("📋 Instructions:");
                    Console.WriteLine("   1. Open 'full_window_grid_*.png' to see full game window");
                    Console.WriteLine("   2. Find captcha 'v o l v' in the image");
                    Console.WriteLine("   3. Use grid to measure coordinates (grid = 50px)");
                    Console.WriteLine("   4. Check suggestion_*.png files to see which captures captcha correctly");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void TestCoordinates(Bitmap fullWindow, Rectangle area, string name, string debugFolder, string timestamp)
        {
            try
            {
                Console.WriteLine($"📍 Testing {name}: X={area.X}, Y={area.Y}, W={area.Width}, H={area.Height}");

                if (area.X + area.Width > fullWindow.Width || area.Y + area.Height > fullWindow.Height)
                {
                    Console.WriteLine($"   ❌ Area extends beyond window bounds!");
                    return;
                }

                // Crop area
                using (var croppedImage = new Bitmap(area.Width, area.Height))
                {
                    using (var g = Graphics.FromImage(croppedImage))
                    {
                        g.DrawImage(fullWindow, new Rectangle(0, 0, area.Width, area.Height), area, GraphicsUnit.Pixel);
                    }

                    // Save cropped image
                    string cropPath = Path.Combine(debugFolder, $"{name}_crop_{timestamp}.png");
                    croppedImage.Save(cropPath);

                    // Analyze colors
                    var analysis = AnalyzeImageColors(croppedImage);
                    Console.WriteLine($"   🎨 Colors: White={analysis.WhiteRatio:P1}, Colored={analysis.ColoredRatio:P1}, Dark={analysis.DarkRatio:P1}");
                    Console.WriteLine($"   💾 Saved: {cropPath}");

                    // Quick assessment
                    if (analysis.ColoredRatio > 0.05 && analysis.ColoredRatio < 0.5 && analysis.WhiteRatio > 0.3)
                    {
                        Console.WriteLine($"   ✅ Looks promising for captcha!");
                    }
                    else if (analysis.WhiteRatio > 0.9)
                    {
                        Console.WriteLine($"   ❌ Mostly white - likely empty area");
                    }
                    else if (analysis.DarkRatio > 0.7)
                    {
                        Console.WriteLine($"   ❌ Mostly dark - likely background");
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️ Mixed colors - check manually");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error testing {name}: {ex.Message}");
            }
        }

        private static Bitmap CreateGridOverlay(Bitmap originalImage)
        {
            var gridImage = new Bitmap(originalImage);
            
            using (var g = Graphics.FromImage(gridImage))
            {
                // Grid với khoảng cách 50 pixels
                var gridPen = new Pen(Color.Red, 1);
                var textBrush = new SolidBrush(Color.Red);
                var font = new Font("Arial", 12, FontStyle.Bold);

                // Vertical lines
                for (int x = 50; x < originalImage.Width; x += 50)
                {
                    g.DrawLine(gridPen, x, 0, x, originalImage.Height);
                    if (x <= originalImage.Width - 30) // Đảm bảo text không bị cắt
                        g.DrawString(x.ToString(), font, textBrush, x + 2, 5);
                }

                // Horizontal lines  
                for (int y = 50; y < originalImage.Height; y += 50)
                {
                    g.DrawLine(gridPen, 0, y, originalImage.Width, y);
                    if (y <= originalImage.Height - 20) // Đảm bảo text không bị cắt
                        g.DrawString(y.ToString(), font, textBrush, 5, y + 2);
                }

                // Highlight current captcha area
                var currentArea = new Config().CaptchaArea;
                var highlightPen = new Pen(Color.Lime, 4);
                g.DrawRectangle(highlightPen, currentArea);
                
                // Label current area
                string label = $"Current: {currentArea.X},{currentArea.Y}";
                var labelBrush = new SolidBrush(Color.Lime);
                g.FillRectangle(new SolidBrush(Color.Black), currentArea.X, currentArea.Y - 25, 120, 20);
                g.DrawString(label, font, labelBrush, currentArea.X, currentArea.Y - 22);
            }

            return gridImage;
        }

        private static (double WhiteRatio, double ColoredRatio, double DarkRatio) AnalyzeImageColors(Bitmap image)
        {
            int whitePixels = 0;
            int coloredPixels = 0; 
            int darkPixels = 0;
            int totalSamples = 0;

            // Sample every 4th pixel để tăng tốc
            for (int x = 0; x < image.Width; x += 4)
            {
                for (int y = 0; y < image.Height; y += 4)
                {
                    totalSamples++;
                    Color pixel = image.GetPixel(x, y);
                    
                    if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
                        whitePixels++;
                    else if (pixel.R < 80 && pixel.G < 80 && pixel.B < 80)
                        darkPixels++;
                    else
                        coloredPixels++;
                }
            }

            return (
                WhiteRatio: (double)whitePixels / totalSamples,
                ColoredRatio: (double)coloredPixels / totalSamples,
                DarkRatio: (double)darkPixels / totalSamples
            );
        }
    }
}
