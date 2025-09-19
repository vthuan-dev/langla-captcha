using System;
using System.Drawing;
using System.IO;
using System.Linq;
using langla_duky.Models;
using Tesseract;

namespace langla_duky
{
    /// <summary>
    /// OCR Debugger - Test OCR riêng biệt trước khi tích hợp vào game
    /// </summary>
    public class OCRDebugger
    {
        private TesseractCaptchaReader _captchaReader;
        private string _testImagesFolder = "test_image";
        private string _debugOutputFolder = "ocr_debug_output";

        public OCRDebugger()
        {
            // Khởi tạo OCR reader
            _captchaReader = new TesseractCaptchaReader("./tessdata", "eng");
            
            // Tạo thư mục test nếu chưa có
            if (!Directory.Exists(_testImagesFolder))
            {
                Directory.CreateDirectory(_testImagesFolder);
                Console.WriteLine($"📁 Đã tạo thư mục test: {_testImagesFolder}");
                Console.WriteLine("💡 Hãy copy các hình ảnh captcha vào thư mục này để test");
            }
            
            if (!Directory.Exists(_debugOutputFolder))
            {
                Directory.CreateDirectory(_debugOutputFolder);
                Console.WriteLine($"📁 Đã tạo thư mục debug output: {_debugOutputFolder}");
            }
        }

        /// <summary>
        /// Test OCR với tất cả hình ảnh trong thư mục test
        /// </summary>
        public void TestAllImages()
        {
            Console.WriteLine("🔍 Bắt đầu test OCR với tất cả hình ảnh...");
            
            var imageFiles = Directory.GetFiles(_testImagesFolder, "*.png")
                .Concat(Directory.GetFiles(_testImagesFolder, "*.jpg"))
                .Concat(Directory.GetFiles(_testImagesFolder, "*.bmp"))
                .ToArray();

            if (imageFiles.Length == 0)
            {
                Console.WriteLine("❌ Không tìm thấy hình ảnh nào trong thư mục test!");
                Console.WriteLine($"📁 Thư mục: {Path.GetFullPath(_testImagesFolder)}");
                return;
            }

            Console.WriteLine($"📸 Tìm thấy {imageFiles.Length} hình ảnh để test");

            int successCount = 0;
            int totalCount = imageFiles.Length;

            foreach (var imagePath in imageFiles)
            {
                Console.WriteLine($"\n{new string('=', 50)}");
                Console.WriteLine($"🖼️ Testing: {Path.GetFileName(imagePath)}");
                Console.WriteLine($"📍 Path: {imagePath}");
                
                var result = TestSingleImage(imagePath);
                if (result.IsSuccess)
                {
                    successCount++;
                    Console.WriteLine($"✅ SUCCESS: '{result.Result}'");
                }
                else
                {
                    Console.WriteLine($"❌ FAILED: {result.ErrorMessage}");
                }
            }

            Console.WriteLine($"\n{new string('=', 50)}");
            Console.WriteLine($"📊 KẾT QUẢ TỔNG QUAN:");
            Console.WriteLine($"✅ Thành công: {successCount}/{totalCount}");
            Console.WriteLine($"❌ Thất bại: {totalCount - successCount}/{totalCount}");
            Console.WriteLine($"📈 Tỷ lệ thành công: {(double)successCount / totalCount * 100:F1}%");
        }

        /// <summary>
        /// Test OCR với một hình ảnh cụ thể
        /// </summary>
        public OCRTestResult TestSingleImage(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    return new OCRTestResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"File không tồn tại: {imagePath}"
                    };
                }

                // Load hình ảnh
                using (var originalImage = new Bitmap(imagePath))
                {
                    Console.WriteLine($"📏 Kích thước hình ảnh: {originalImage.Width}x{originalImage.Height}");
                    
                    // Test với hình ảnh gốc
                    Console.WriteLine("🔍 Testing với hình ảnh gốc...");
                    string originalResult = _captchaReader.ReadCaptcha(originalImage);
                    Console.WriteLine($"📝 Kết quả gốc: '{originalResult}'");
                    
                    // Test với các phương pháp xử lý khác nhau
                    var preprocessingResults = TestWithPreprocessing(originalImage, Path.GetFileNameWithoutExtension(imagePath));
                    
                    // Tìm kết quả tốt nhất
                    var bestResult = FindBestResult(originalResult, preprocessingResults);
                    
                    return new OCRTestResult
                    {
                        IsSuccess = !string.IsNullOrEmpty(bestResult) && _captchaReader.IsValidCaptcha(bestResult),
                        Result = bestResult,
                        ErrorMessage = string.IsNullOrEmpty(bestResult) ? "Không đọc được text" : "OK",
                        AllResults = preprocessingResults.Prepend(originalResult).ToArray()
                    };
                }
            }
            catch (Exception ex)
            {
                return new OCRTestResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Test với các phương pháp xử lý hình ảnh khác nhau
        /// </summary>
        private string[] TestWithPreprocessing(Bitmap originalImage, string baseFileName)
        {
            var results = new List<string>();
            var methods = new (string methodName, Func<Bitmap> preprocessingFunc)[]
            {
                ("Enhanced Preprocessing", () => ScreenCapture.PreprocessCaptchaImage(originalImage)),
                ("Pink/Magenta Isolation", () => ScreenCapture.IsolatePinkMagentaText(originalImage)),
                ("Grayscale + Threshold", () => {
                    var gray = ScreenCapture.ConvertToGrayscale(originalImage);
                    var threshold = ScreenCapture.ApplyThreshold(gray, 150);
                    gray.Dispose();
                    return threshold;
                }),
                ("Scale 2x", () => ScreenCapture.ScaleImage(originalImage, 2.0f)),
                ("Scale 3x", () => ScreenCapture.ScaleImage(originalImage, 3.0f)),
                ("Scale 4x", () => ScreenCapture.ScaleImage(originalImage, 4.0f)),
                ("Adaptive Threshold", () => {
                    var gray = ScreenCapture.ConvertToGrayscale(originalImage);
                    var threshold = ScreenCapture.ApplyAdaptiveThreshold(gray);
                    gray.Dispose();
                    return threshold;
                })
            };

            foreach (var method in methods)
            {
                try
                {
                    Console.WriteLine($"🔧 Testing {method.methodName}...");
                    
                    using (var processedImage = method.preprocessingFunc())
                    {
                        // Lưu hình ảnh đã xử lý để debug
                        string debugPath = Path.Combine(_debugOutputFolder, $"{baseFileName}_{method.methodName.Replace(" ", "_")}.png");
                        processedImage.Save(debugPath);
                        Console.WriteLine($"💾 Saved: {debugPath}");
                        
                        // Test OCR
                        string result = _captchaReader.ReadCaptcha(processedImage);
                        Console.WriteLine($"📝 Result: '{result}'");
                        
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in {method.methodName}: {ex.Message}");
                    results.Add(string.Empty);
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Tìm kết quả tốt nhất từ các phương pháp khác nhau
        /// </summary>
        private string FindBestResult(string originalResult, string[] preprocessingResults)
        {
            var allResults = preprocessingResults.Prepend(originalResult).ToArray();
            
            // Ưu tiên kết quả hợp lệ (4 ký tự)
            foreach (var result in allResults)
            {
                if (!string.IsNullOrEmpty(result) && _captchaReader.IsValidCaptcha(result))
                {
                    return result;
                }
            }
            
            // Nếu không có kết quả hợp lệ, trả về kết quả không rỗng đầu tiên
            foreach (var result in allResults)
            {
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Test với một hình ảnh từ clipboard hoặc đường dẫn cụ thể
        /// </summary>
        public void TestSpecificImage(string imagePath)
        {
            Console.WriteLine($"🔍 Testing hình ảnh cụ thể: {imagePath}");
            
            var result = TestSingleImage(imagePath);
            
            Console.WriteLine($"\n{new string('=', 50)}");
            Console.WriteLine($"📊 KẾT QUẢ:");
            Console.WriteLine($"✅ Thành công: {result.IsSuccess}");
            Console.WriteLine($"📝 Kết quả: '{result.Result}'");
            Console.WriteLine($"📋 Tất cả kết quả: [{string.Join(", ", result.AllResults.Select(r => $"'{r}'"))}]");
            
            if (!result.IsSuccess)
            {
                Console.WriteLine($"❌ Lỗi: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// Tạo hình ảnh test mẫu
        /// </summary>
        public void CreateSampleTestImages()
        {
            Console.WriteLine("🎨 Tạo hình ảnh test mẫu...");
            
            var samples = new[]
            {
                ("Sample1", "A1B2"),
                ("Sample2", "C3D4"),
                ("Sample3", "E5F6"),
                ("Sample4", "G7H8")
            };

            foreach (var (name, text) in samples)
            {
                CreateSampleImage(name, text);
            }
            
            Console.WriteLine($"✅ Đã tạo {samples.Length} hình ảnh mẫu trong thư mục {_testImagesFolder}");
        }

        private void CreateSampleImage(string name, string text)
        {
            try
            {
                int width = 200;
                int height = 80;
                
                using (var bitmap = new Bitmap(width, height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // Background trắng
                    graphics.Clear(Color.White);
                    
                    // Vẽ text màu hồng/magenta
                    using (var font = new Font("Arial", 24, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.Magenta))
                    {
                        var textSize = graphics.MeasureString(text, font);
                        float x = (width - textSize.Width) / 2;
                        float y = (height - textSize.Height) / 2;
                        
                        graphics.DrawString(text, font, brush, x, y);
                    }
                    
                    // Thêm noise để giống captcha thật
                    var random = new Random();
                    for (int i = 0; i < 50; i++)
                    {
                        int x = random.Next(width);
                        int y = random.Next(height);
                        Color noiseColor = Color.FromArgb(128, random.Next(256), random.Next(256), random.Next(256));
                        bitmap.SetPixel(x, y, noiseColor);
                    }
                    
                    string filePath = Path.Combine(_testImagesFolder, $"{name}.png");
                    bitmap.Save(filePath);
                    Console.WriteLine($"💾 Created: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating {name}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _captchaReader?.Dispose();
        }
    }

    /// <summary>
    /// Kết quả test OCR
    /// </summary>
    public class OCRTestResult
    {
        public bool IsSuccess { get; set; }
        public string Result { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string[] AllResults { get; set; } = Array.Empty<string>();
    }
}
