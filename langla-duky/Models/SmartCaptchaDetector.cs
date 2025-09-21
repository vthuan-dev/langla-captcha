using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenCvSharp;

namespace langla_duky.Models
{
    /// <summary>
    /// Smart captcha detector using multiple heuristics and pattern recognition
    /// </summary>
    public class SmartCaptchaDetector
    {
        private readonly List<CaptchaPattern> _knownPatterns;
        private readonly Dictionary<string, int> _detectionHistory;
        private readonly int _maxHistorySize = 100;

        public SmartCaptchaDetector()
        {
            _knownPatterns = new List<CaptchaPattern>();
            _detectionHistory = new Dictionary<string, int>();
            InitializeKnownPatterns();
        }

        private void InitializeKnownPatterns()
        {
            // Add known captcha patterns based on your game
            _knownPatterns.Add(new CaptchaPattern
            {
                Name = "Standard 4-char captcha",
                MinWidth = 120,
                MaxWidth = 200,
                MinHeight = 40,
                MaxHeight = 80,
                AspectRatioMin = 2.5,
                AspectRatioMax = 5.0,
                ExpectedColors = new[] { "white", "light_gray" },
                TextDensityMin = 0.1,
                TextDensityMax = 0.4,
                ConfidenceWeight = 1.0
            });

            _knownPatterns.Add(new CaptchaPattern
            {
                Name = "Wide captcha",
                MinWidth = 200,
                MaxWidth = 400,
                MinHeight = 50,
                MaxHeight = 100,
                AspectRatioMin = 3.0,
                AspectRatioMax = 6.0,
                ExpectedColors = new[] { "white", "light_gray" },
                TextDensityMin = 0.08,
                TextDensityMax = 0.35,
                ConfidenceWeight = 0.8
            });
        }

        public CaptchaDetectionResult DetectCaptcha(Mat screenshot, ROIDetectionResult roiResult)
        {
            var result = new CaptchaDetectionResult
            {
                IsCaptcha = false,
                Confidence = 0.0,
                Reasoning = new List<string>()
            };

            if (!roiResult.Success)
            {
                result.Reasoning.Add("ROI detection failed");
                return result;
            }

            var region = roiResult.Region;
            var confidence = roiResult.Confidence;

            // Extract region for analysis
            using var roi = new Mat(screenshot, new OpenCvSharp.Rect(region.X, region.Y, region.Width, region.Height));
            
            // Apply multiple detection heuristics
            var heuristics = new List<(string name, double score, string reason)>
            {
                AnalyzeSizeAndAspectRatio(region),
                AnalyzeColorDistribution(roi),
                AnalyzeTextDensity(roi),
                AnalyzeEdgePatterns(roi),
                AnalyzeBrightnessPatterns(roi),
                CheckAgainstKnownPatterns(region, roi),
                AnalyzeHistoricalPatterns(region)
            };

            // Calculate weighted confidence
            double totalScore = 0.0;
            double totalWeight = 0.0;

            foreach (var (name, score, reason) in heuristics)
            {
                if (score > 0)
                {
                    totalScore += score;
                    totalWeight += 1.0;
                    result.Reasoning.Add($"{name}: {reason} (score: {score:F1})");
                }
            }

            if (totalWeight > 0)
            {
                result.Confidence = (totalScore / totalWeight) * 100.0;
                result.IsCaptcha = result.Confidence >= 60.0; // Threshold for captcha detection
            }

            // Update detection history
            UpdateDetectionHistory(region, result.IsCaptcha);

            return result;
        }

        private (string name, double score, string reason) AnalyzeSizeAndAspectRatio(Rectangle region)
        {
            double aspectRatio = (double)region.Width / region.Height;
            
            // Captcha typically has width:height ratio between 2.5-6.0
            if (aspectRatio >= 2.5 && aspectRatio <= 6.0)
            {
                // Score based on how close to ideal ratio (4.0)
                double score = 1.0 - Math.Abs(aspectRatio - 4.0) / 2.0;
                return ("Size/Aspect", score * 100, $"Good aspect ratio {aspectRatio:F1}");
            }
            
            return ("Size/Aspect", 0, $"Poor aspect ratio {aspectRatio:F1}");
        }

        private (string name, double score, string reason) AnalyzeColorDistribution(Mat roi)
        {
            using var hsv = new Mat();
            Cv2.CvtColor(roi, hsv, ColorConversionCodes.BGR2HSV);

            // Count white/light pixels (typical captcha background)
            using var mask = new Mat();
            var lowerWhite = new Scalar(0, 0, 200);
            var upperWhite = new Scalar(180, 30, 255);
            Cv2.InRange(hsv, lowerWhite, upperWhite, mask);

            var whitePixels = Cv2.CountNonZero(mask);
            var totalPixels = roi.Rows * roi.Cols;
            var whiteRatio = (double)whitePixels / totalPixels;

            if (whiteRatio > 0.3) // At least 30% white background
            {
                double score = Math.Min(whiteRatio * 2, 1.0) * 100;
                return ("Color", score, $"Good white background {whiteRatio:P1}");
            }

            return ("Color", 0, $"Poor white background {whiteRatio:P1}");
        }

        private (string name, double score, string reason) AnalyzeTextDensity(Mat roi)
        {
            using var gray = new Mat();
            Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);

            // Use adaptive threshold to find text
            using var binary = new Mat();
            Cv2.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdTypes.GaussianC, 
                ThresholdTypes.Binary, 11, 2);

            var textPixels = Cv2.CountNonZero(binary);
            var totalPixels = roi.Rows * roi.Cols;
            var textDensity = (double)textPixels / totalPixels;

            // Captcha typically has 10-40% text density
            if (textDensity >= 0.1 && textDensity <= 0.4)
            {
                double score = 1.0 - Math.Abs(textDensity - 0.25) / 0.15;
                return ("TextDensity", score * 100, $"Good text density {textDensity:P1}");
            }

            return ("TextDensity", 0, $"Poor text density {textDensity:P1}");
        }

        private (string name, double score, string reason) AnalyzeEdgePatterns(Mat roi)
        {
            using var gray = new Mat();
            Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);

            using var edges = new Mat();
            Cv2.Canny(gray, edges, 50, 150);

            // Count edge pixels
            var edgePixels = Cv2.CountNonZero(edges);
            var totalPixels = roi.Rows * roi.Cols;
            var edgeDensity = (double)edgePixels / totalPixels;

            // Captcha typically has moderate edge density (5-20%)
            if (edgeDensity >= 0.05 && edgeDensity <= 0.20)
            {
                double score = 1.0 - Math.Abs(edgeDensity - 0.12) / 0.08;
                return ("Edges", score * 100, $"Good edge density {edgeDensity:P1}");
            }

            return ("Edges", 0, $"Poor edge density {edgeDensity:P1}");
        }

        private (string name, double score, string reason) AnalyzeBrightnessPatterns(Mat roi)
        {
            using var gray = new Mat();
            Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);

            // Calculate brightness statistics
            var mean = Cv2.Mean(gray);
            var stddev = new Scalar();
            Cv2.MeanStdDev(gray, out mean, out stddev);

            // Captcha typically has high brightness (white background) with moderate variation
            if (mean.Val0 > 180 && stddev.Val0 > 20 && stddev.Val0 < 80)
            {
                double brightnessScore = (mean.Val0 - 180) / 75.0; // 0-1 range
                double variationScore = 1.0 - Math.Abs(stddev.Val0 - 50) / 30.0; // 0-1 range
                double score = (brightnessScore + variationScore) / 2.0 * 100;
                
                return ("Brightness", score, $"Good brightness {mean.Val0:F0}±{stddev.Val0:F0}");
            }

            return ("Brightness", 0, $"Poor brightness {mean.Val0:F0}±{stddev.Val0:F0}");
        }

        private (string name, double score, string reason) CheckAgainstKnownPatterns(Rectangle region, Mat roi)
        {
            foreach (var pattern in _knownPatterns)
            {
                if (MatchesPattern(region, roi, pattern))
                {
                    return ("Pattern", pattern.ConfidenceWeight * 100, $"Matches '{pattern.Name}'");
                }
            }

            return ("Pattern", 0, "No known pattern match");
        }

        private bool MatchesPattern(Rectangle region, Mat roi, CaptchaPattern pattern)
        {
            // Check size constraints
            if (region.Width < pattern.MinWidth || region.Width > pattern.MaxWidth) return false;
            if (region.Height < pattern.MinHeight || region.Height > pattern.MaxHeight) return false;

            // Check aspect ratio
            double aspectRatio = (double)region.Width / region.Height;
            if (aspectRatio < pattern.AspectRatioMin || aspectRatio > pattern.AspectRatioMax) return false;

            // Check text density
            using var gray = new Mat();
            Cv2.CvtColor(roi, gray, ColorConversionCodes.BGR2GRAY);
            
            using var binary = new Mat();
            Cv2.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdTypes.GaussianC, 
                ThresholdTypes.Binary, 11, 2);
            
            var textPixels = Cv2.CountNonZero(binary);
            var totalPixels = roi.Rows * roi.Cols;
            var textDensity = (double)textPixels / totalPixels;
            
            if (textDensity < pattern.TextDensityMin || textDensity > pattern.TextDensityMax) return false;

            return true;
        }

        private (string name, double score, string reason) AnalyzeHistoricalPatterns(Rectangle region)
        {
            var regionKey = $"{region.Width}x{region.Height}";
            
            if (_detectionHistory.ContainsKey(regionKey))
            {
                var successCount = _detectionHistory[regionKey];
                if (successCount > 3) // If this size was successful multiple times
                {
                    double score = Math.Min(successCount / 10.0, 1.0) * 100;
                    return ("History", score, $"Size {regionKey} successful {successCount} times");
                }
            }

            return ("History", 0, $"No history for size {regionKey}");
        }

        private void UpdateDetectionHistory(Rectangle region, bool wasCaptcha)
        {
            var regionKey = $"{region.Width}x{region.Height}";
            
            if (wasCaptcha)
            {
                if (_detectionHistory.ContainsKey(regionKey))
                {
                    _detectionHistory[regionKey]++;
                }
                else
                {
                    _detectionHistory[regionKey] = 1;
                }
            }

            // Clean up old entries
            if (_detectionHistory.Count > _maxHistorySize)
            {
                var oldestKeys = _detectionHistory.OrderBy(kvp => kvp.Value).Take(_detectionHistory.Count - _maxHistorySize).Select(kvp => kvp.Key).ToList();
                foreach (var key in oldestKeys)
                {
                    _detectionHistory.Remove(key);
                }
            }
        }
    }

    public class CaptchaDetectionResult
    {
        public bool IsCaptcha { get; set; }
        public double Confidence { get; set; }
        public List<string> Reasoning { get; set; } = new List<string>();
    }

    public class CaptchaPattern
    {
        public string Name { get; set; } = "";
        public int MinWidth { get; set; }
        public int MaxWidth { get; set; }
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public double AspectRatioMin { get; set; }
        public double AspectRatioMax { get; set; }
        public string[] ExpectedColors { get; set; } = Array.Empty<string>();
        public double TextDensityMin { get; set; }
        public double TextDensityMax { get; set; }
        public double ConfidenceWeight { get; set; }
    }
}
