using System;
using System.Drawing;
using System.Drawing.Imaging; // System.Drawing.Imaging.ImageFormat
using System.IO;
using System.Linq;
using Tesseract; // Tesseract.ImageFormat
using langla_duky.Models;

namespace langla_duky
{
    /// <summary>
    /// Bộ xử lý đặc biệt cho captcha của Duke Client
    /// </summary>
    public class DukeCaptchaProcessor
    {
        private readonly string _tessDataPath;
        private readonly string _language;
        
        public DukeCaptchaProcessor(string tessDataPath = "./tessdata", string language = "eng")
        {
            _tessDataPath = tessDataPath;
            _language = language;
        }
        
        /// <summary>
        /// Xử lý captcha kiểu Duke Client với các ký tự có khoảng trắng
        /// </summary>
        public string ProcessDukeCaptcha(Bitmap captchaImage)
        {
            Console.WriteLine("🎮 Bắt đầu xử lý captcha Duke Client...");
            
            try
            {
                // Lưu ảnh gốc để debug
                string debugFolder = "duke_captcha_debug";
                Directory.CreateDirectory(debugFolder);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string originalPath = Path.Combine(debugFolder, $"duke_original_{timestamp}.png");
                captchaImage.Save(originalPath, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"✅ Đã lưu ảnh gốc: {originalPath}");
                
                // Kết quả từ các phương pháp
                string result = "";
                
                // Phương pháp 1: Xử lý màu đỏ/cam đặc biệt cho Duke Client
                using (var processedImage = ProcessRedOrangeText(captchaImage))
                {
                    string processedPath = Path.Combine(debugFolder, $"duke_processed_{timestamp}.png");
                    processedImage.Save(processedPath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"✅ Đã lưu ảnh đã xử lý: {processedPath}");
                    
                    // Xử lý OCR với cấu hình đặc biệt cho captcha có khoảng trắng
                    result = RecognizeWithSpacedConfig(processedImage);
                    Console.WriteLine($"📝 Kết quả OCR phương pháp 1: '{result}'");
                    
                    if (!string.IsNullOrEmpty(result) && result.Length >= 3)
                        return result;
                }
                
                // Phương pháp 2: Scale lên và tăng độ tương phản
                using (var scaledImage = ScaleAndEnhance(captchaImage, 3.0f))
                {
                    string scaledPath = Path.Combine(debugFolder, $"duke_scaled_{timestamp}.png");
                    scaledImage.Save(scaledPath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"✅ Đã lưu ảnh đã scale: {scaledPath}");
                    
                    // Xử lý OCR với cấu hình đặc biệt cho captcha có khoảng trắng
                    result = RecognizeWithSpacedConfig(scaledImage);
                    Console.WriteLine($"📝 Kết quả OCR phương pháp 2: '{result}'");
                    
                    if (!string.IsNullOrEmpty(result) && result.Length >= 3)
                        return result;
                }
                
                // Phương pháp 3: Xử lý đặc biệt cho captcha có khoảng trắng
                using (var spacedImage = ProcessSpacedCaptcha(captchaImage))
                {
                    string spacedPath = Path.Combine(debugFolder, $"duke_spaced_{timestamp}.png");
                    spacedImage.Save(spacedPath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"✅ Đã lưu ảnh xử lý khoảng trắng: {spacedPath}");
                    
                    // Xử lý OCR với cấu hình đặc biệt cho captcha có khoảng trắng
                    result = RecognizeWithSpacedConfig(spacedImage);
                    Console.WriteLine($"📝 Kết quả OCR phương pháp 3: '{result}'");
                    
                    if (!string.IsNullOrEmpty(result) && result.Length >= 3)
                        return result;
                }
                
                // Phương pháp 4: Trích xuất từng ký tự riêng biệt
                result = ExtractIndividualCharacters(captchaImage, debugFolder, timestamp);
                Console.WriteLine($"📝 Kết quả OCR phương pháp 4: '{result}'");
                
                if (!string.IsNullOrEmpty(result) && result.Length >= 3)
                    return result;
                
                // Nếu tất cả phương pháp thất bại, trả về kết quả rỗng
                Console.WriteLine("❌ Tất cả phương pháp đều thất bại");
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi xử lý captcha Duke Client: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return "";
            }
        }
        
        /// <summary>
        /// Xử lý màu đỏ/cam đặc biệt cho captcha của Duke Client
        /// </summary>
        private Bitmap ProcessRedOrangeText(Bitmap originalImage)
        {
            Bitmap result = new Bitmap(originalImage.Width, originalImage.Height);
            
            for (int x = 0; x < originalImage.Width; x++)
            {
                for (int y = 0; y < originalImage.Height; y++)
                {
                    Color pixel = originalImage.GetPixel(x, y);
                    
                    // Nhận diện màu đỏ/cam (như trong captcha của Duke Client)
                    bool isRedOrange = (pixel.R > 180 && pixel.G < 150 && pixel.B < 100) ||  // Đỏ/cam
                                      (pixel.R > 200 && pixel.G > 100 && pixel.G < 180 && pixel.B < 100) ||  // Cam
                                      (pixel.R > 150 && pixel.G < 100 && pixel.B > 100);  // Màu hồng/tím
                    
                    if (isRedOrange)
                    {
                        result.SetPixel(x, y, Color.Black); // Chuyển thành đen cho OCR
                    }
                    else
                    {
                        result.SetPixel(x, y, Color.White); // Nền trắng
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Scale ảnh lên và tăng độ tương phản
        /// </summary>
        private Bitmap ScaleAndEnhance(Bitmap originalImage, float scale)
        {
            // Scale ảnh lên
            Bitmap scaledImage = new Bitmap((int)(originalImage.Width * scale), (int)(originalImage.Height * scale));
            using (Graphics g = Graphics.FromImage(scaledImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, scaledImage.Width, scaledImage.Height);
            }
            
            // Tăng độ tương phản
            Bitmap enhancedImage = new Bitmap(scaledImage.Width, scaledImage.Height);
            float contrast = 2.0f; // Tăng độ tương phản
            
            using (Graphics g = Graphics.FromImage(enhancedImage))
            {
                ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {contrast, 0, 0, 0, 0},
                    new float[] {0, contrast, 0, 0, 0},
                    new float[] {0, 0, contrast, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {-0.5f, -0.5f, -0.5f, 0, 1}
                });
                
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);
                
                g.DrawImage(scaledImage, new Rectangle(0, 0, scaledImage.Width, scaledImage.Height),
                    0, 0, scaledImage.Width, scaledImage.Height, GraphicsUnit.Pixel, attributes);
            }
            
            scaledImage.Dispose();
            return enhancedImage;
        }
        
        /// <summary>
        /// Xử lý đặc biệt cho captcha có khoảng trắng
        /// </summary>
        private Bitmap ProcessSpacedCaptcha(Bitmap originalImage)
        {
            Bitmap result = new Bitmap(originalImage.Width, originalImage.Height);
            
            // Tạo ảnh với nền trắng và text đen để tăng độ tương phản
            for (int x = 0; x < originalImage.Width; x++)
            {
                for (int y = 0; y < originalImage.Height; y++)
                {
                    Color pixel = originalImage.GetPixel(x, y);
                    
                    // Nhận diện màu đỏ/cam/hồng (như trong captcha của Duke Client)
                    bool isColoredText = (pixel.R > 180 && pixel.G < 150) ||  // Đỏ/cam
                                        (pixel.R > 200 && pixel.G > 100 && pixel.G < 180) ||  // Cam
                                        (pixel.R > 150 && pixel.G < 100 && pixel.B > 100);  // Màu hồng/tím
                    
                    if (isColoredText)
                    {
                        result.SetPixel(x, y, Color.Black); // Chuyển thành đen cho OCR
                    }
                    else
                    {
                        result.SetPixel(x, y, Color.White); // Nền trắng
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Trích xuất từng ký tự riêng biệt
        /// </summary>
        private string ExtractIndividualCharacters(Bitmap originalImage, string debugFolder, string timestamp)
        {
            try
            {
                Console.WriteLine("🔍 Trích xuất từng ký tự riêng biệt...");
                
                // Tìm các vùng ký tự
                var characterRegions = FindCharacterRegions(originalImage);
                Console.WriteLine($"✅ Đã tìm thấy {characterRegions.Count} vùng ký tự");
                
                if (characterRegions.Count == 0)
                    return "";
                
                // Tạo ảnh với các ký tự được tách biệt
                Bitmap separatedImage = new Bitmap(originalImage.Width, originalImage.Height);
                using (Graphics g = Graphics.FromImage(separatedImage))
                {
                    g.Clear(Color.White);
                    
                    // Vẽ từng ký tự với màu đen đậm
                    for (int i = 0; i < characterRegions.Count; i++)
                    {
                        var region = characterRegions[i];
                        
                        // Cắt ký tự từ ảnh gốc
                        Bitmap charImage = new Bitmap(region.Width, region.Height);
                        using (Graphics charG = Graphics.FromImage(charImage))
                        {
                            charG.DrawImage(originalImage, 
                                new Rectangle(0, 0, region.Width, region.Height),
                                region, 
                                GraphicsUnit.Pixel);
                        }
                        
                        // Lưu ảnh ký tự để debug
                        string charPath = Path.Combine(debugFolder, $"duke_char_{i}_{timestamp}.png");
                        charImage.Save(charPath, System.Drawing.Imaging.ImageFormat.Png);
                        
                        // Vẽ ký tự vào ảnh kết quả
                        g.FillRectangle(Brushes.Black, 
                            new Rectangle(region.X, region.Y, region.Width, region.Height));
                    }
                }
                
                // Lưu ảnh đã tách ký tự để debug
                string separatedPath = Path.Combine(debugFolder, $"duke_separated_{timestamp}.png");
                separatedImage.Save(separatedPath, System.Drawing.Imaging.ImageFormat.Png);
                
                // Nhận dạng ảnh đã tách ký tự
                string result = RecognizeWithSpacedConfig(separatedImage);
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi trích xuất ký tự: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// Tìm các vùng ký tự trong ảnh
        /// </summary>
        private System.Collections.Generic.List<Rectangle> FindCharacterRegions(Bitmap image)
        {
            var regions = new System.Collections.Generic.List<Rectangle>();
            
            // Tìm các pixel có màu sắc (ký tự)
            for (int x = 0; x < image.Width; x += 5) // Bước nhảy 5 để tăng tốc độ
            {
                for (int y = 0; y < image.Height; y += 5)
                {
                    Color pixel = image.GetPixel(x, y);
                    
                    // Nhận diện màu đỏ/cam/hồng (như trong captcha của Duke Client)
                    bool isColoredText = (pixel.R > 180 && pixel.G < 150) ||  // Đỏ/cam
                                        (pixel.R > 200 && pixel.G > 100 && pixel.G < 180) ||  // Cam
                                        (pixel.R > 150 && pixel.G < 100 && pixel.B > 100);  // Màu hồng/tím
                    
                    if (isColoredText)
                    {
                        // Tìm vùng ký tự xung quanh pixel này
                        Rectangle charRegion = FindCharacterRegion(image, x, y);
                        
                        // Chỉ thêm vùng có kích thước hợp lý
                        if (charRegion.Width > 5 && charRegion.Height > 5)
                        {
                            // Kiểm tra xem vùng này có trùng với vùng đã tìm thấy không
                            bool isDuplicate = false;
                            foreach (var region in regions)
                            {
                                if (Math.Abs(region.X - charRegion.X) < 10 && Math.Abs(region.Y - charRegion.Y) < 10)
                                {
                                    isDuplicate = true;
                                    break;
                                }
                            }
                            
                            if (!isDuplicate)
                            {
                                regions.Add(charRegion);
                            }
                        }
                    }
                }
            }
            
            // Sắp xếp các vùng từ trái sang phải
            return regions.OrderBy(r => r.X).ToList();
        }
        
        /// <summary>
        /// Tìm vùng ký tự xung quanh một pixel
        /// </summary>
        private Rectangle FindCharacterRegion(Bitmap image, int startX, int startY)
        {
            int minX = startX, maxX = startX, minY = startY, maxY = startY;
            
            // Tìm ranh giới của ký tự
            for (int x = Math.Max(0, startX - 15); x < Math.Min(image.Width, startX + 15); x++)
            {
                for (int y = Math.Max(0, startY - 15); y < Math.Min(image.Height, startY + 15); y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    
                    // Nhận diện màu đỏ/cam/hồng (như trong captcha của Duke Client)
                    bool isColoredText = (pixel.R > 180 && pixel.G < 150) ||  // Đỏ/cam
                                        (pixel.R > 200 && pixel.G > 100 && pixel.G < 180) ||  // Cam
                                        (pixel.R > 150 && pixel.G < 100 && pixel.B > 100);  // Màu hồng/tím
                    
                    if (isColoredText)
                    {
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }
            
            // Mở rộng vùng một chút để đảm bảo bao trọn ký tự
            minX = Math.Max(0, minX - 2);
            minY = Math.Max(0, minY - 2);
            maxX = Math.Min(image.Width - 1, maxX + 2);
            maxY = Math.Min(image.Height - 1, maxY + 2);
            
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        
        /// <summary>
        /// Nhận dạng OCR với cấu hình đặc biệt cho captcha có khoảng trắng
        /// </summary>
        private string RecognizeWithSpacedConfig(Bitmap image)
        {
            try
            {
                string result = "";
                
                // Lưu ảnh tạm thời để tránh lỗi heap corruption
                string tempImagePath = Path.Combine(Path.GetTempPath(), $"duke_temp_{Guid.NewGuid()}.png");
                image.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
                
                using (var engine = new TesseractEngine(_tessDataPath, _language, EngineMode.Default))
                {
                    // Cấu hình đặc biệt cho captcha có khoảng trắng
                    engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ");
                    engine.SetVariable("preserve_interword_spaces", "1");
                    engine.SetVariable("tessedit_pageseg_mode", "7"); // Treat as single line
                    engine.SetVariable("tessedit_do_invert", "0");
                    engine.SetVariable("textord_tabfind_show_vlines", "0");
                    
                    // Sử dụng Pix.LoadFromFile để tránh lỗi heap corruption
                    using (var pix = Pix.LoadFromFile(tempImagePath))
                    {
                        using (var page = engine.Process(pix))
                        {
                            result = page.GetText().Trim();
                        }
                    }
                }
                
                // Xóa file tạm
                try { File.Delete(tempImagePath); } catch { }
                
                // Xử lý kết quả
                result = CleanupResult(result);
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi OCR: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// Làm sạch kết quả OCR
        /// </summary>
        private string CleanupResult(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            // Loại bỏ ký tự đặc biệt
            string cleaned = new string(text.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
            
            // Loại bỏ khoảng trắng thừa
            while (cleaned.Contains("  "))
                cleaned = cleaned.Replace("  ", " ");
            
            cleaned = cleaned.Trim();
            
            return cleaned;
        }
    }
}
