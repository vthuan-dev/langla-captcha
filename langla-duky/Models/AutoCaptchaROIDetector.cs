using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    public class ROIDetectionResult
    {
        public System.Drawing.Rectangle Region { get; set; }
        public double Confidence { get; set; }
        public string Method { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public string DebugInfo { get; set; } = string.Empty;
    }

    public class AutoCaptchaROIDetector
    {
        private readonly bool _enableDebugOutput;
        private readonly string _debugOutputPath;

        public AutoCaptchaROIDetector(bool enableDebugOutput = true, string debugOutputPath = "captcha_debug")
        {
            _enableDebugOutput = enableDebugOutput;
            _debugOutputPath = debugOutputPath;
            
            if (_enableDebugOutput)
            {
                Directory.CreateDirectory(_debugOutputPath);
            }
        }

        public ROIDetectionResult DetectCaptchaRegion(Mat fullScreenshot)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ROIDetectionResult { Success = false };

            try
            {
                if (fullScreenshot.Empty())
                {
                    result.DebugInfo = "Input screenshot is empty";
                    return result;
                }

                // Try multiple detection methods
                result = DetectWithMultipleMethods(fullScreenshot);
                result.ProcessingTime = stopwatch.Elapsed;

                return result;
            }
            catch (Exception ex)
            {
                result.DebugInfo = $"Detection error: {ex.Message}";
                result.ProcessingTime = stopwatch.Elapsed;
                return result;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public ROIDetectionResult DetectWithMultipleMethods(Mat screenshot)
        {
            var results = new List<ROIDetectionResult>();

            // Method 1: Contour Detection (Primary)
            try
            {
                var contourResult = DetectWithContours(screenshot);
                if (contourResult.Success)
                {
                    results.Add(contourResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Contour detection failed: {ex.Message}");
            }

            // Method 2: Color-based Detection
            try
            {
                var colorResult = DetectWithColorAnalysis(screenshot);
                if (colorResult.Success)
                {
                    results.Add(colorResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Color detection failed: {ex.Message}");
            }

            // Method 3: Edge Detection (Fallback)
            try
            {
                var edgeResult = DetectWithEdges(screenshot);
                if (edgeResult.Success)
                {
                    results.Add(edgeResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Edge detection failed: {ex.Message}");
            }

            // Method 4: High Contrast Detection (New)
            try
            {
                var contrastResult = DetectWithHighContrast(screenshot);
                if (contrastResult.Success)
                {
                    results.Add(contrastResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"High contrast detection failed: {ex.Message}");
            }

            // Return best result based on confidence
            if (results.Count == 0)
            {
                return new ROIDetectionResult
                {
                    Success = false,
                    DebugInfo = "All detection methods failed"
                };
            }

            var bestResult = results.OrderByDescending(r => r.Confidence).First();
            bestResult.DebugInfo = $"Best of {results.Count} methods: {string.Join(", ", results.Select(r => $"{r.Method}({r.Confidence:F1})"))}";
            
            return bestResult;
        }

        private ROIDetectionResult DetectWithContours(Mat screenshot)
        {
            var result = new ROIDetectionResult { Method = "Contour Detection" };
            
            using var gray = new Mat();
            Cv2.CvtColor(screenshot, gray, ColorConversionCodes.BGR2GRAY);

            using var binary = new Mat();
            Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // Save debug images
            if (_enableDebugOutput)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                gray.SaveImage(Path.Combine(_debugOutputPath, $"contour_gray_{timestamp}.png"));
                binary.SaveImage(Path.Combine(_debugOutputPath, $"contour_binary_{timestamp}.png"));
            }

            Cv2.FindContours(binary, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var candidates = new List<(System.Drawing.Rectangle rect, double score)>();
            int totalContours = contours.Length;
            int filteredOut = 0;

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                var filteredRect = FilterByGameCharacteristics(rect, screenshot.Size());
                
                if (filteredRect.Width > 0 && filteredRect.Height > 0)
                {
                    var confidence = CalculateConfidence(filteredRect, screenshot);
                    candidates.Add((filteredRect, confidence));
                }
                else
                {
                    filteredOut++;
                }
            }

            if (_enableDebugOutput)
            {
                Console.WriteLine($"Contour Detection: Found {totalContours} contours, {filteredOut} filtered out, {candidates.Count} candidates");
            }

            if (candidates.Count > 0)
            {
                var best = candidates.OrderByDescending(c => c.score).First();
                result.Region = best.rect;
                result.Confidence = best.score;
                result.Success = true;
                result.DebugInfo = $"Found {candidates.Count} candidates, best score: {best.score:F1}";
            }
            else
            {
                result.DebugInfo = "No valid contours found";
            }

            return result;
        }

        private ROIDetectionResult DetectWithColorAnalysis(Mat screenshot)
        {
            var result = new ROIDetectionResult { Method = "Color Analysis" };

            using var hsv = new Mat();
            Cv2.CvtColor(screenshot, hsv, ColorConversionCodes.BGR2HSV);

            // Detect white/bright regions (captcha background) - more strict
            using var mask = new Mat();
            var lowerWhite = new Scalar(0, 0, 220);    // Higher threshold for white
            var upperWhite = new Scalar(180, 20, 255); // More strict saturation
            Cv2.InRange(hsv, lowerWhite, upperWhite, mask);

            // Morphological operations to clean up
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
            using var cleaned = new Mat();
            Cv2.MorphologyEx(mask, cleaned, MorphTypes.Close, kernel);

            // Save debug images
            if (_enableDebugOutput)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                mask.SaveImage(Path.Combine(_debugOutputPath, $"color_mask_{timestamp}.png"));
                cleaned.SaveImage(Path.Combine(_debugOutputPath, $"color_cleaned_{timestamp}.png"));
            }

            Cv2.FindContours(cleaned, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var candidates = new List<(System.Drawing.Rectangle rect, double score)>();
            int totalContours = contours.Length;
            int filteredOut = 0;

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                var filteredRect = FilterByGameCharacteristics(rect, screenshot.Size());
                
                if (filteredRect.Width > 0 && filteredRect.Height > 0)
                {
                    var confidence = CalculateConfidence(filteredRect, screenshot);
                    candidates.Add((filteredRect, confidence));
                }
                else
                {
                    filteredOut++;
                }
            }

            if (_enableDebugOutput)
            {
                Console.WriteLine($"Color Analysis: Found {totalContours} contours, {filteredOut} filtered out, {candidates.Count} candidates");
            }

            if (candidates.Count > 0)
            {
                var best = candidates.OrderByDescending(c => c.score).First();
                result.Region = best.rect;
                result.Confidence = best.score;
                result.Success = true;
                result.DebugInfo = $"Found {candidates.Count} color candidates, best score: {best.score:F1}";
            }
            else
            {
                result.DebugInfo = "No valid color regions found";
            }

            return result;
        }

        private ROIDetectionResult DetectWithEdges(Mat screenshot)
        {
            var result = new ROIDetectionResult { Method = "Edge Detection" };

            using var gray = new Mat();
            Cv2.CvtColor(screenshot, gray, ColorConversionCodes.BGR2GRAY);

            using var edges = new Mat();
            Cv2.Canny(gray, edges, 50, 150);

            // Morphological closing to connect edges
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            using var closed = new Mat();
            Cv2.MorphologyEx(edges, closed, MorphTypes.Close, kernel);

            // Save debug images
            if (_enableDebugOutput)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                edges.SaveImage(Path.Combine(_debugOutputPath, $"edge_canny_{timestamp}.png"));
                closed.SaveImage(Path.Combine(_debugOutputPath, $"edge_closed_{timestamp}.png"));
            }

            Cv2.FindContours(closed, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var candidates = new List<(System.Drawing.Rectangle rect, double score)>();
            int totalContours = contours.Length;
            int filteredOut = 0;

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                var filteredRect = FilterByGameCharacteristics(rect, screenshot.Size());
                
                if (filteredRect.Width > 0 && filteredRect.Height > 0)
                {
                    var confidence = CalculateConfidence(filteredRect, screenshot);
                    candidates.Add((filteredRect, confidence));
                }
                else
                {
                    filteredOut++;
                }
            }

            if (_enableDebugOutput)
            {
                Console.WriteLine($"Edge Detection: Found {totalContours} contours, {filteredOut} filtered out, {candidates.Count} candidates");
            }

            if (candidates.Count > 0)
            {
                var best = candidates.OrderByDescending(c => c.score).First();
                result.Region = best.rect;
                result.Confidence = best.score;
                result.Success = true;
                result.DebugInfo = $"Found {candidates.Count} edge candidates, best score: {best.score:F1}";
            }
            else
            {
                result.DebugInfo = "No valid edge regions found";
            }

            return result;
        }

        private ROIDetectionResult DetectWithHighContrast(Mat screenshot)
        {
            var result = new ROIDetectionResult { Method = "High Contrast" };

            using var gray = new Mat();
            Cv2.CvtColor(screenshot, gray, ColorConversionCodes.BGR2GRAY);

            // Calculate local contrast using Laplacian
            using var laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);
            
            using var contrast = new Mat();
            Cv2.ConvertScaleAbs(laplacian, contrast);

            // Threshold to find high contrast regions
            using var binary = new Mat();
            Cv2.Threshold(contrast, binary, 30, 255, ThresholdTypes.Binary);

            // Save debug images
            if (_enableDebugOutput)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                contrast.SaveImage(Path.Combine(_debugOutputPath, $"contrast_laplacian_{timestamp}.png"));
                binary.SaveImage(Path.Combine(_debugOutputPath, $"contrast_binary_{timestamp}.png"));
            }

            Cv2.FindContours(binary, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var candidates = new List<(System.Drawing.Rectangle rect, double score)>();
            int totalContours = contours.Length;
            int filteredOut = 0;

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                var filteredRect = FilterByGameCharacteristics(rect, screenshot.Size());
                
                if (filteredRect.Width > 0 && filteredRect.Height > 0)
                {
                    var confidence = CalculateConfidence(filteredRect, screenshot);
                    candidates.Add((filteredRect, confidence));
                }
                else
                {
                    filteredOut++;
                }
            }

            if (_enableDebugOutput)
            {
                Console.WriteLine($"High Contrast: Found {totalContours} contours, {filteredOut} filtered out, {candidates.Count} candidates");
            }

            if (candidates.Count > 0)
            {
                var best = candidates.OrderByDescending(c => c.score).First();
                result.Region = best.rect;
                result.Confidence = best.score;
                result.Success = true;
                result.DebugInfo = $"Found {candidates.Count} contrast candidates, best score: {best.score:F1}";
            }
            else
            {
                result.DebugInfo = "No valid contrast regions found";
            }

            return result;
        }

        private System.Drawing.Rectangle FilterByGameCharacteristics(OpenCvSharp.Rect rect, OpenCvSharp.Size screenSize)
        {
            // More flexible size validation: 100-500px width, 30-150px height
            if (rect.Width < 100 || rect.Width > 500) return System.Drawing.Rectangle.Empty;
            if (rect.Height < 30 || rect.Height > 150) return System.Drawing.Rectangle.Empty;

            // More flexible aspect ratio validation: 1.5-8.0
            double aspectRatio = (double)rect.Width / rect.Height;
            if (aspectRatio < 1.5 || aspectRatio > 8.0) return System.Drawing.Rectangle.Empty;

            // More flexible position validation: avoid only extreme edges (10%-90% of screen)
            if (rect.X < screenSize.Width * 0.1) return System.Drawing.Rectangle.Empty;
            if (rect.Y < screenSize.Height * 0.1) return System.Drawing.Rectangle.Empty;
            if (rect.X > screenSize.Width * 0.9) return System.Drawing.Rectangle.Empty;
            if (rect.Y > screenSize.Height * 0.9) return System.Drawing.Rectangle.Empty;

            return new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        private double CalculateConfidence(System.Drawing.Rectangle region, Mat image)
        {
            double score = 0.0;

            // Size score (prefer medium sizes)
            double sizeScore = 1.0 - Math.Abs(region.Width - 300) / 300.0;
            sizeScore = Math.Max(0, Math.Min(1, sizeScore));
            score += sizeScore * 0.3;

            // Aspect ratio score (prefer 3.5-4.5)
            double aspectRatio = (double)region.Width / region.Height;
            double aspectScore = 1.0 - Math.Abs(aspectRatio - 4.0) / 2.0;
            aspectScore = Math.Max(0, Math.Min(1, aspectScore));
            score += aspectScore * 0.2;

            // Position score (prefer center-right area)
            double centerX = (double)region.X / image.Width;
            double centerY = (double)region.Y / image.Height;
            double positionScore = 1.0 - Math.Abs(centerX - 0.6) / 0.4; // Prefer 60% from left
            positionScore = Math.Max(0, Math.Min(1, positionScore));
            score += positionScore * 0.2;

            // Brightness score (prefer white backgrounds)
            using var roi = new Mat(image, new OpenCvSharp.Rect(region.X, region.Y, region.Width, region.Height));
            using var gray = new Mat();
            Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
            
            var mean = Cv2.Mean(gray);
            double brightnessScore = mean.Val0 / 255.0;
            score += brightnessScore * 0.3;

            return score * 100; // Convert to percentage
        }

        public void SaveDebugImage(Mat image, System.Drawing.Rectangle region, string method)
        {
            if (!_enableDebugOutput) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var debugImage = image.Clone();
                
                // Draw bounding box
                Cv2.Rectangle(debugImage, new OpenCvSharp.Rect(region.X, region.Y, region.Width, region.Height), Scalar.Red, 2);
                Cv2.PutText(debugImage, $"{method}: {region.Width}x{region.Height}", 
                    new OpenCvSharp.Point(region.X, region.Y - 10), HersheyFonts.HersheySimplex, 0.7, Scalar.Red, 2);

                debugImage.SaveImage(Path.Combine(_debugOutputPath, $"debug_{method}_{timestamp}.png"));
                debugImage.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save debug image: {ex.Message}");
            }
        }
    }
}