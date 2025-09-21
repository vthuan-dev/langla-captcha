using System;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    public class CaptchaTemplateManager
    {
        private readonly string _templatePath;
        private readonly bool _enableDebugOutput;
        
        public CaptchaTemplateManager(string templatePath = "captcha_templates", bool enableDebugOutput = true)
        {
            _templatePath = templatePath;
            _enableDebugOutput = enableDebugOutput;
            
            // Create template directory
            Directory.CreateDirectory(_templatePath);
            
            // Create default templates if they don't exist
            CreateDefaultTemplates();
        }
        
        private void CreateDefaultTemplates()
        {
            try
            {
                // Check if templates already exist
                var windowTemplatePath = Path.Combine(_templatePath, "captcha_window.png");
                var backgroundTemplatePath = Path.Combine(_templatePath, "captcha_background.png");
                var borderTemplatePath = Path.Combine(_templatePath, "captcha_border.png");
                
                if (File.Exists(windowTemplatePath) && 
                    File.Exists(backgroundTemplatePath) && 
                    File.Exists(borderTemplatePath))
                {
                    Console.WriteLine("✅ Captcha templates already exist");
                    return;
                }
                
                Console.WriteLine("📸 Creating default captcha templates...");
                
                // Create a default captcha window template (wooden frame with white background)
                CreateDefaultCaptchaWindow(windowTemplatePath);
                CreateDefaultCaptchaBackground(backgroundTemplatePath);
                CreateDefaultCaptchaBorder(borderTemplatePath);
                
                Console.WriteLine("✅ Default captcha templates created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating default templates: {ex.Message}");
            }
        }
        
        private void CreateDefaultCaptchaWindow(string filePath)
        {
            // Create a default captcha window (300x80 pixels)
            using var window = new Mat(80, 300, MatType.CV_8UC3, new Scalar(139, 69, 19)); // Brown background
            
            // Draw wooden border
            Cv2.Rectangle(window, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(299, 79), 
                         new Scalar(101, 67, 33), 3); // Darker brown border
            
            // Draw white captcha area (inner rectangle)
            Cv2.Rectangle(window, new OpenCvSharp.Point(20, 20), new OpenCvSharp.Point(279, 59), 
                         new Scalar(255, 255, 255), -1); // White background
            
            // Draw some decorative corners
            Cv2.Circle(window, new OpenCvSharp.Point(10, 10), 8, new Scalar(255, 215, 0), -1); // Gold corner
            Cv2.Circle(window, new OpenCvSharp.Point(289, 10), 8, new Scalar(255, 215, 0), -1);
            Cv2.Circle(window, new OpenCvSharp.Point(10, 69), 8, new Scalar(255, 215, 0), -1);
            Cv2.Circle(window, new OpenCvSharp.Point(289, 69), 8, new Scalar(255, 215, 0), -1);
            
            // Add title bar
            Cv2.Rectangle(window, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(299, 25), 
                         new Scalar(160, 82, 45), -1); // Darker brown title bar
            
            // Save template
            Cv2.ImWrite(filePath, window);
            Console.WriteLine($"💾 Created default captcha window template: {filePath}");
        }
        
        private void CreateDefaultCaptchaBackground(string filePath)
        {
            // Create white background template (260x40 pixels)
            using var background = new Mat(40, 260, MatType.CV_8UC3, new Scalar(255, 255, 255));
            
            // Add some subtle noise to make it more realistic
            using var noise = new Mat();
            Cv2.Randn(noise, new Scalar(0), new Scalar(10));
            background.ConvertTo(background, MatType.CV_8UC3);
            
            // Save template
            Cv2.ImWrite(filePath, background);
            Console.WriteLine($"💾 Created default captcha background template: {filePath}");
        }
        
        private void CreateDefaultCaptchaBorder(string filePath)
        {
            // Create border template (300x80 pixels)
            using var border = new Mat(80, 300, MatType.CV_8UC3, new Scalar(139, 69, 19)); // Brown background
            
            // Draw wooden border
            Cv2.Rectangle(border, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(299, 79), 
                         new Scalar(101, 67, 33), 3); // Darker brown border
            
            // Draw decorative corners
            Cv2.Circle(border, new OpenCvSharp.Point(10, 10), 8, new Scalar(255, 215, 0), -1); // Gold corner
            Cv2.Circle(border, new OpenCvSharp.Point(289, 10), 8, new Scalar(255, 215, 0), -1);
            Cv2.Circle(border, new OpenCvSharp.Point(10, 69), 8, new Scalar(255, 215, 0), -1);
            Cv2.Circle(border, new OpenCvSharp.Point(289, 69), 8, new Scalar(255, 215, 0), -1);
            
            // Add title bar
            Cv2.Rectangle(border, new OpenCvSharp.Point(0, 0), new OpenCvSharp.Point(299, 25), 
                         new Scalar(160, 82, 45), -1); // Darker brown title bar
            
            // Save template
            Cv2.ImWrite(filePath, border);
            Console.WriteLine($"💾 Created default captcha border template: {filePath}");
        }
        
        // Method to check if templates exist
        public bool HasTemplates()
        {
            var windowTemplatePath = Path.Combine(_templatePath, "captcha_window.png");
            var backgroundTemplatePath = Path.Combine(_templatePath, "captcha_background.png");
            var borderTemplatePath = Path.Combine(_templatePath, "captcha_border.png");
            
            return File.Exists(windowTemplatePath) && 
                   File.Exists(backgroundTemplatePath) && 
                   File.Exists(borderTemplatePath);
        }
        
        // Method to get template info
        public string GetTemplateInfo()
        {
            if (!HasTemplates())
            {
                return "❌ No captcha templates found";
            }
            
            var windowTemplatePath = Path.Combine(_templatePath, "captcha_window.png");
            var backgroundTemplatePath = Path.Combine(_templatePath, "captcha_background.png");
            var borderTemplatePath = Path.Combine(_templatePath, "captcha_border.png");
            
            var windowInfo = GetImageInfo(windowTemplatePath);
            var backgroundInfo = GetImageInfo(backgroundTemplatePath);
            var borderInfo = GetImageInfo(borderTemplatePath);
            
            return $"✅ Captcha templates found:\n" +
                   $"📸 Window: {windowInfo}\n" +
                   $"🎨 Background: {backgroundInfo}\n" +
                   $"🖼️ Border: {borderInfo}";
        }
        
        private string GetImageInfo(string imagePath)
        {
            try
            {
                using var image = Cv2.ImRead(imagePath);
                if (image.Empty())
                {
                    return "❌ Invalid image";
                }
                
                return $"{image.Width}x{image.Height} pixels, {new FileInfo(imagePath).Length} bytes";
            }
            catch
            {
                return "❌ Error reading image";
            }
        }
        
        // Method to delete templates (for testing)
        public void DeleteTemplates()
        {
            try
            {
                var files = new[]
                {
                    Path.Combine(_templatePath, "captcha_window.png"),
                    Path.Combine(_templatePath, "captcha_background.png"),
                    Path.Combine(_templatePath, "captcha_border.png")
                };
                
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        Console.WriteLine($"🗑️ Deleted: {file}");
                    }
                }
                
                Console.WriteLine("✅ All templates deleted");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting templates: {ex.Message}");
            }
        }
    }
}
