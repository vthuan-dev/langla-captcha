using System;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using Tesseract;

namespace langla_duky
{
    /// <summary>
    /// Test class for debugging OCR methods with specific CAPTCHA images
    /// </summary>
    public class TestOCRMethods
    {
        private TesseractEngine? _tessEngine;
        private readonly string _debugOutputPath = "captcha_debug";

        public TestOCRMethods()
        {
            InitializeTesseract();
            if (!Directory.Exists(_debugOutputPath))
            {
                Directory.CreateDirectory(_debugOutputPath);
            }
        }

        private void InitializeTesseract()
        {
            try
            {
                _tessEngine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
                _tessEngine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                _tessEngine.SetVariable("tessedit_pageseg_mode", "7"); // Single text line
                _tessEngine.SetVariable("tessedit_ocr_engine_mode", "1"); // Neural nets LSTM only
                _tessEngine.SetVariable("classify_bln_numeric_mode", "0");
                _tessEngine.SetVariable("tessedit_char_blacklist", "!@#$%^&*()_+-=[]{}|;':\",./<>?`~");
                _tessEngine.SetVariable("tessedit_do_invert", "0");
                _tessEngine.SetVariable("textord_min_linesize", "2.5");
                Console.WriteLine("✅ Test Tesseract engine initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize Tesseract: {ex.Message}");
            }
        }

        /// <summary>
        /// Test all OCR methods on a specific CAPTCHA image
        /// </summary>
        public void TestAllMethods(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"❌ Image not found: {imagePath}");
                return;
            }

            Console.WriteLine($"🔍 Testing OCR methods on: {imagePath}");
            
            using var bmp = new Bitmap(imagePath);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();
            using var matColor = Cv2.ImDecode(bytes, ImreadModes.Color);
            
            if (matColor.Empty())
            {
                Console.WriteLine("❌ Failed to load image");
                return;
            }

            // Convert to grayscale
            using var matGray = new Mat();
            Cv2.CvtColor(matColor, matGray, ColorConversionCodes.BGR2GRAY);

            // Test different methods
            TestMethod("Original", matGray);
            TestMethod("Otsu", ApplyOtsu(matGray));
            TestMethod("OtsuInv", ApplyOtsuInv(matGray));
            TestMethod("AdaptiveMean", ApplyAdaptiveMean(matGray));
            TestMethod("ColorMask", ApplyColorMask(matColor, matGray));
            TestMethod("HighContrast", ApplyHighContrast(matGray));
            TestMethod("OrangeMask", ApplyOrangeMask(matColor, matGray));
            TestMethod("EdgeDetection", ApplyEdgeDetection(matGray));
        }

        private void TestMethod(string methodName, Mat inputMat)
        {
            try
            {
                var result = TryOCR(inputMat, methodName);
                Console.WriteLine($"📝 {methodName}: '{result}'");
                
                // Save debug image
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                inputMat.SaveImage(Path.Combine(_debugOutputPath, $"test_{methodName}_{timestamp}.png"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {methodName} error: {ex.Message}");
            }
        }

        private string TryOCR(Mat binaryMat, string method)
        {
            if (_tessEngine == null) return string.Empty;

            try
            {
                // Resize if too small
                Mat processedMat = binaryMat;
                if (binaryMat.Width < 200 || binaryMat.Height < 50)
                {
                    var scale = Math.Max(200.0 / binaryMat.Width, 50.0 / binaryMat.Height);
                    var newWidth = (int)(binaryMat.Width * scale);
                    var newHeight = (int)(binaryMat.Height * scale);
                    
                    processedMat = new Mat();
                    Cv2.Resize(binaryMat, processedMat, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Nearest);
                }

                using var binBmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(processedMat);
                using var ms2 = new MemoryStream();
                binBmp.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                var pix = Pix.LoadFromMemory(ms2.ToArray());
                using (pix)
                {
                    using var page = _tessEngine.Process(pix);
                    var text = page.GetText().Trim();
                    var originalText = text;
                    text = new string(text.Where(char.IsLetterOrDigit).ToArray());
                    
                    return text;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OCR {method} error: {ex.Message}");
                return string.Empty;
            }
        }

        // Image processing methods
        private Mat ApplyOtsu(Mat gray)
        {
            var result = new Mat();
            Cv2.Threshold(gray, result, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            return result;
        }

        private Mat ApplyOtsuInv(Mat gray)
        {
            var result = new Mat();
            Cv2.Threshold(gray, result, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
            return result;
        }

        private Mat ApplyAdaptiveMean(Mat gray)
        {
            var result = new Mat();
            Cv2.AdaptiveThreshold(gray, result, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 11, 2);
            return result;
        }

        private Mat ApplyColorMask(Mat color, Mat gray)
        {
            var result = new Mat();
            using var matHSV = new Mat();
            Cv2.CvtColor(color, matHSV, ColorConversionCodes.BGR2HSV);
            using var mask = new Mat();
            Cv2.InRange(matHSV, new Scalar(0, 0, 0), new Scalar(30, 255, 200), mask);
            Cv2.BitwiseNot(mask, mask);
            Cv2.BitwiseAnd(gray, gray, result, mask);
            Cv2.Threshold(result, result, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            return result;
        }

        private Mat ApplyHighContrast(Mat gray)
        {
            var result = new Mat();
            using var enhanced = new Mat();
            Cv2.ConvertScaleAbs(gray, enhanced, 2.0, -100);
            Cv2.Threshold(enhanced, result, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            return result;
        }

        private Mat ApplyOrangeMask(Mat color, Mat gray)
        {
            var result = new Mat();
            using var matLab = new Mat();
            Cv2.CvtColor(color, matLab, ColorConversionCodes.BGR2Lab);
            using var orangeMask = new Mat();
            Cv2.InRange(matLab, new Scalar(0, 100, 100), new Scalar(255, 150, 150), orangeMask);
            Cv2.BitwiseNot(orangeMask, orangeMask);
            Cv2.BitwiseAnd(gray, gray, result, orangeMask);
            Cv2.Threshold(result, result, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            return result;
        }

        private Mat ApplyEdgeDetection(Mat gray)
        {
            var result = new Mat();
            using var edges = new Mat();
            Cv2.Canny(gray, edges, 50, 150);
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            Cv2.Dilate(edges, result, kernel);
            Cv2.Threshold(result, result, 0, 255, ThresholdTypes.Binary);
            return result;
        }

        public void Dispose()
        {
            _tessEngine?.Dispose();
        }
    }
}
