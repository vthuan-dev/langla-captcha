using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace langla_duky.Models
{
    public class MockTesseractCaptchaReader : IDisposable
    {
        private bool _disposed = false;
        private readonly string _tessDataPath;
        private readonly string _language;
        private readonly Config? _config;

        public MockTesseractCaptchaReader(string tessDataPath, string language, Config? config = null)
        {
            _tessDataPath = tessDataPath;
            _language = language;
            _config = config;
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            try
            {
                Console.WriteLine("🔧 Mock Tesseract Engine initialized (no real Tesseract)");
                Console.WriteLine($"🔧 TessData Path: {_tessDataPath}");
                Console.WriteLine($"🔧 Language: {_language}");
                Console.WriteLine("✅ Mock engine ready for testing");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Mock engine initialization error: {ex.Message}");
            }
        }

        public string ReadCaptcha(Bitmap captchaImage)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MockTesseractCaptchaReader));
            }

            try
            {
                Console.WriteLine("\n=== MOCK OCR PROCESSING ===");
                Console.WriteLine($"Image size: {captchaImage.Width}x{captchaImage.Height}");
                
                // Lưu hình ảnh để debug
                SaveDebugImage(captchaImage, "mock_original");
                
                // Mock OCR - return common captcha patterns
                var commonPatterns = new[] { "dnla", "test", "1234", "abcd", "captcha", "xyz", "abc", "def" };
                
                // Chọn pattern dựa trên thời gian để có variation
                var index = DateTime.Now.Second % commonPatterns.Length;
                var mockResult = commonPatterns[index];
                
                Console.WriteLine($"🔧 Mock OCR result: '{mockResult}'");
                
                // Validate result
                if (IsValidCaptcha(mockResult))
                {
                    Console.WriteLine("✅ Mock captcha validation passed!");
                    return mockResult;
                }
                else
                {
                    Console.WriteLine("❌ Mock captcha validation failed");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Mock OCR Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return string.Empty;
            }
        }

        public bool IsValidCaptcha(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            var cleanText = CleanCaptchaText(text);
            if (string.IsNullOrEmpty(cleanText)) return false;
            
            // Mock validation - accept any non-empty text
            bool isValidLength = cleanText.Length >= 1 && cleanText.Length <= 10;
            bool hasValidCharacters = cleanText.All(c => char.IsLetterOrDigit(c));
            
            Console.WriteLine($"🔧 Mock validation: '{cleanText}' -> Length: {isValidLength}, ValidChars: {hasValidCharacters}");
            
            return isValidLength && hasValidCharacters;
        }

        public void SetVariable(string name, string value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MockTesseractCaptchaReader));
            }
            
            Console.WriteLine($"🔧 Mock SetVariable: {name} = {value}");
        }

        private string CleanCaptchaText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            // Remove common OCR artifacts
            var cleaned = text.Trim()
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace(" ", "")
                .Replace("\t", "");
            
            // Keep only alphanumeric characters
            cleaned = new string(cleaned.Where(c => char.IsLetterOrDigit(c)).ToArray());
            
            return cleaned;
        }

        private void SaveDebugImage(Bitmap image, string prefix)
        {
            try
            {
                string debugFolder = "debug_images";
                Directory.CreateDirectory(debugFolder);
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string filename = Path.Combine(debugFolder, $"{prefix}_{timestamp}.png");
                
                image.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"🔧 Mock debug image saved: {filename}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Mock debug image save error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Console.WriteLine("🔧 Mock TesseractCaptchaReader disposed");
                _disposed = true;
            }
        }
    }
}
