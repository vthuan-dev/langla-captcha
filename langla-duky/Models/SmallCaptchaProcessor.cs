using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    /// <summary>
    /// Specialized processor for small captcha images (like the jClO example)
    /// Optimized for better OCR accuracy on small, colorful captcha regions
    /// </summary>
    public class SmallCaptchaProcessor
    {
        private readonly bool _enableDebugOutput;
        private readonly string _debugOutputPath;

        public SmallCaptchaProcessor(bool enableDebugOutput = true, string debugOutputPath = "captcha_debug")
        {
            _enableDebugOutput = enableDebugOutput;
            _debugOutputPath = debugOutputPath;
            
            if (_enableDebugOutput)
            {
                Directory.CreateDirectory(_debugOutputPath);
            }
        }

        /// <summary>
        /// Process small captcha with specialized methods
        /// </summary>
        public Mat ProcessSmallCaptcha(Mat inputImage)
        {
            if (inputImage.Empty())
                return inputImage;

            Console.WriteLine($"🔍 Processing small captcha: {inputImage.Width}x{inputImage.Height}");

            // Method 1: High-resolution scaling with sharpening
            var scaled = ProcessWithHighResScaling(inputImage);
            if (!scaled.Empty())
            {
                Console.WriteLine("✅ High-res scaling successful");
                return scaled;
            }

            // Method 2: Color separation with contrast enhancement
            var colorSeparated = ProcessWithColorSeparation(inputImage);
            if (!colorSeparated.Empty())
            {
                Console.WriteLine("✅ Color separation successful");
                return colorSeparated;
            }

            // Method 3: Multi-threshold approach
            var multiThreshold = ProcessWithMultiThreshold(inputImage);
            if (!multiThreshold.Empty())
            {
                Console.WriteLine("✅ Multi-threshold successful");
                return multiThreshold;
            }

            // Fallback: return original
            Console.WriteLine("⚠️ All small captcha methods failed, returning original");
            return inputImage;
        }

        /// <summary>
        /// High-resolution scaling with sharpening for small captcha
        /// </summary>
        private Mat ProcessWithHighResScaling(Mat input)
        {
            try
            {
                // Scale up significantly (4x) for better OCR
                using var scaled = new Mat();
                Cv2.Resize(input, scaled, new OpenCvSharp.Size(input.Width * 4, input.Height * 4), 
                    interpolation: InterpolationFlags.Cubic);

                // Apply sharpening filter
                using var kernel = new Mat(3, 3, MatType.CV_32F, new float[]
                {
                    0, -1, 0,
                    -1, 5, -1,
                    0, -1, 0
                });

                using var sharpened = new Mat();
                Cv2.Filter2D(scaled, sharpened, -1, kernel);

                // Convert to grayscale
                using var gray = new Mat();
                Cv2.CvtColor(sharpened, gray, ColorConversionCodes.BGR2GRAY);

                // Apply adaptive threshold
                using var binary = new Mat();
                Cv2.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdTypes.MeanC, 
                    ThresholdTypes.Binary, 11, 2);

                // Save debug image
                if (_enableDebugOutput)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    binary.SaveImage(Path.Combine(_debugOutputPath, $"small_highres_{timestamp}.png"));
                }

                return binary.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"High-res scaling failed: {ex.Message}");
                return new Mat();
            }
        }

        /// <summary>
        /// Color separation with contrast enhancement
        /// </summary>
        private Mat ProcessWithColorSeparation(Mat input)
        {
            try
            {
                // Convert to HSV for better color separation
                using var hsv = new Mat();
                Cv2.CvtColor(input, hsv, ColorConversionCodes.BGR2HSV);

                // Create masks for different color ranges
                var masks = new List<Mat>();
                var colorRanges = new[]
                {
                    // Red/Orange range
                    (new Scalar(0, 50, 50), new Scalar(10, 255, 255)),
                    (new Scalar(170, 50, 50), new Scalar(180, 255, 255)),
                    // Green range
                    (new Scalar(40, 50, 50), new Scalar(80, 255, 255)),
                    // Blue range
                    (new Scalar(100, 50, 50), new Scalar(130, 255, 255)),
                    // Yellow range
                    (new Scalar(20, 50, 50), new Scalar(40, 255, 255))
                };

                foreach (var (lower, upper) in colorRanges)
                {
                    using var mask = new Mat();
                    Cv2.InRange(hsv, lower, upper, mask);
                    masks.Add(mask.Clone());
                }

                // Combine all masks
                using var combined = new Mat();
                Cv2.BitwiseOr(masks[0], masks[1], combined);
                for (int i = 2; i < masks.Count; i++)
                {
                    using var temp = combined.Clone();
                    Cv2.BitwiseOr(temp, masks[i], combined);
                }

                // Apply morphological operations
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
                using var cleaned = new Mat();
                Cv2.MorphologyEx(combined, cleaned, MorphTypes.Close, kernel);

                // Convert to 3-channel for processing
                using var result = new Mat();
                Cv2.CvtColor(cleaned, result, ColorConversionCodes.GRAY2BGR);

                // Apply contrast enhancement
                using var enhanced = new Mat();
                result.ConvertTo(enhanced, -1, 2.0, 0); // alpha=2.0 for contrast

                // Convert back to grayscale
                using var gray = new Mat();
                Cv2.CvtColor(enhanced, gray, ColorConversionCodes.BGR2GRAY);

                // Apply threshold
                using var binary = new Mat();
                Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                // Save debug image
                if (_enableDebugOutput)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    binary.SaveImage(Path.Combine(_debugOutputPath, $"small_colorsep_{timestamp}.png"));
                }

                // Clean up masks
                foreach (var mask in masks)
                    mask.Dispose();

                return binary.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Color separation failed: {ex.Message}");
                return new Mat();
            }
        }

        /// <summary>
        /// Multi-threshold approach for difficult captcha
        /// </summary>
        private Mat ProcessWithMultiThreshold(Mat input)
        {
            try
            {
                using var gray = new Mat();
                Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);

                // Try multiple threshold values
                var thresholds = new[] { 100, 120, 140, 160, 180 };
                var results = new List<Mat>();

                foreach (var thresh in thresholds)
                {
                    using var binary = new Mat();
                    Cv2.Threshold(gray, binary, thresh, 255, ThresholdTypes.Binary);
                    results.Add(binary.Clone());
                }

                // Also try adaptive threshold
                using var adaptive = new Mat();
                Cv2.AdaptiveThreshold(gray, adaptive, 255, AdaptiveThresholdTypes.MeanC, 
                    ThresholdTypes.Binary, 11, 2);
                results.Add(adaptive.Clone());

                // Try Otsu threshold
                using var otsu = new Mat();
                Cv2.Threshold(gray, otsu, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(otsu.Clone());

                // Find the best result (most text-like)
                var bestResult = FindBestThresholdResult(results, gray);

                // Save debug image
                if (_enableDebugOutput)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    bestResult.SaveImage(Path.Combine(_debugOutputPath, $"small_multithresh_{timestamp}.png"));
                }

                // Clean up
                foreach (var result in results)
                    result.Dispose();

                return bestResult.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Multi-threshold failed: {ex.Message}");
                return new Mat();
            }
        }

        /// <summary>
        /// Find the best threshold result based on text characteristics
        /// </summary>
        private Mat FindBestThresholdResult(List<Mat> results, Mat original)
        {
            var bestScore = 0.0;
            var bestResult = results[0];

            foreach (var result in results)
            {
                // Calculate score based on:
                // 1. Number of connected components (should be around 4 for 4-character captcha)
                // 2. Aspect ratio of components
                // 3. Contrast

                using var labels = new Mat();
                using var stats = new Mat();
                using var centroids = new Mat();
                var numComponents = Cv2.ConnectedComponentsWithStats(result, labels, stats, centroids);

                // Score based on number of components (prefer 4-6)
                var componentScore = 1.0 - Math.Abs(numComponents - 5) / 5.0;
                componentScore = Math.Max(0, componentScore);

                // Score based on contrast
                var mean = Cv2.Mean(result);
                var contrastScore = Math.Abs(mean.Val0 - 127) / 127.0;

                var totalScore = componentScore * 0.6 + contrastScore * 0.4;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestResult = result;
                }
            }

            return bestResult;
        }

        public void Dispose()
        {
            // Clean up resources if needed
        }
    }
}
