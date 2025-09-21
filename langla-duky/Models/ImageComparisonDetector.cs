using System;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    public class ImageComparisonDetector
    {
        private readonly string _templatePath;
        private readonly bool _enableDebugOutput;
        private readonly string _debugOutputPath;
        private readonly CaptchaTemplateManager _templateManager;
        
        // Template images for comparison
        private Mat? _captchaWindowTemplate;
        private Mat? _captchaBackgroundTemplate;
        private Mat? _captchaBorderTemplate;
        
        public ImageComparisonDetector(string templatePath = "captcha_templates", bool enableDebugOutput = true)
        {
            _templatePath = templatePath;
            _enableDebugOutput = enableDebugOutput;
            _debugOutputPath = "captcha_debug";
            _templateManager = new CaptchaTemplateManager(templatePath, enableDebugOutput);
            
            // Create directories
            Directory.CreateDirectory(_templatePath);
            Directory.CreateDirectory(_debugOutputPath);
            
            LoadTemplates();
        }
        
        private void LoadTemplates()
        {
            try
            {
                // Load captcha window template (full popup window)
                var windowTemplatePath = Path.Combine(_templatePath, "captcha_window.png");
                if (File.Exists(windowTemplatePath))
                {
                    _captchaWindowTemplate = Cv2.ImRead(windowTemplatePath);
                    Console.WriteLine($"✅ Loaded captcha window template: {windowTemplatePath}");
                }
                
                // Load captcha background template (white background area)
                var backgroundTemplatePath = Path.Combine(_templatePath, "captcha_background.png");
                if (File.Exists(backgroundTemplatePath))
                {
                    _captchaBackgroundTemplate = Cv2.ImRead(backgroundTemplatePath);
                    Console.WriteLine($"✅ Loaded captcha background template: {backgroundTemplatePath}");
                }
                
                // Load captcha border template (wooden border)
                var borderTemplatePath = Path.Combine(_templatePath, "captcha_border.png");
                if (File.Exists(borderTemplatePath))
                {
                    _captchaBorderTemplate = Cv2.ImRead(borderTemplatePath);
                    Console.WriteLine($"✅ Loaded captcha border template: {borderTemplatePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading templates: {ex.Message}");
            }
        }
        
        public ROIDetectionResult DetectWithImageComparison(Mat screenshot)
        {
            var result = new ROIDetectionResult { Method = "Image Comparison" };
            
            if (_captchaWindowTemplate == null)
            {
                result.DebugInfo = "No captcha window template loaded";
                return result;
            }
            
            try
            {
                // Method 1: Template Matching for full window
                var windowResult = DetectWithTemplateMatching(screenshot, _captchaWindowTemplate, "Full Window");
                if (windowResult.Success)
                {
                    return windowResult;
                }
                
                // Method 2: Template Matching for background only
                if (_captchaBackgroundTemplate != null)
                {
                    var backgroundResult = DetectWithTemplateMatching(screenshot, _captchaBackgroundTemplate, "Background Only");
                    if (backgroundResult.Success)
                    {
                        return backgroundResult;
                    }
                }
                
                // Method 3: Template Matching for border only
                if (_captchaBorderTemplate != null)
                {
                    var borderResult = DetectWithTemplateMatching(screenshot, _captchaBorderTemplate, "Border Only");
                    if (borderResult.Success)
                    {
                        return borderResult;
                    }
                }
                
                // Method 4: Color-based detection (white background)
                var colorResult = DetectWithColorMatching(screenshot);
                if (colorResult.Success)
                {
                    return colorResult;
                }
                
                result.DebugInfo = "No captcha detected with image comparison";
            }
            catch (Exception ex)
            {
                result.DebugInfo = $"Image comparison error: {ex.Message}";
            }
            
            return result;
        }
        
        private ROIDetectionResult DetectWithTemplateMatching(Mat screenshot, Mat template, string methodName)
        {
            var result = new ROIDetectionResult { Method = $"Template Matching - {methodName}" };
            
            using var resultMat = new Mat();
            Cv2.MatchTemplate(screenshot, template, resultMat, TemplateMatchModes.CCoeffNormed);
            
            // Find the best match
            Cv2.MinMaxLoc(resultMat, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);
            
            // Save debug images
            if (_enableDebugOutput)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                resultMat.SaveImage(Path.Combine(_debugOutputPath, $"template_match_{methodName.ToLower().Replace(" ", "_")}_{timestamp}.png"));
            }
            
            // Threshold for matching (0.7 = 70% similarity)
            double threshold = 0.7;
            if (maxVal >= threshold)
            {
                var region = new System.Drawing.Rectangle(maxLoc.X, maxLoc.Y, template.Width, template.Height);
                var confidence = maxVal * 100; // Convert to percentage
                
                result.Region = region;
                result.Confidence = confidence;
                result.Success = true;
                result.DebugInfo = $"Template match found with {confidence:F1}% similarity";
                
                Console.WriteLine($"🎯 Template Matching ({methodName}): Found at {region}, Confidence: {confidence:F1}%");
            }
            else
            {
                result.DebugInfo = $"Template match below threshold ({maxVal:F3} < {threshold})";
            }
            
            return result;
        }
        
        private ROIDetectionResult DetectWithColorMatching(Mat screenshot)
        {
            var result = new ROIDetectionResult { Method = "Color Matching" };
            
            // Convert to HSV for better color detection
            using var hsv = new Mat();
            Cv2.CvtColor(screenshot, hsv, ColorConversionCodes.BGR2HSV);
            
            // Detect white/bright regions (captcha background)
            using var mask = new Mat();
            var lowerWhite = new Scalar(0, 0, 200);    // Lower threshold for white
            var upperWhite = new Scalar(180, 30, 255); // Higher saturation tolerance
            Cv2.InRange(hsv, lowerWhite, upperWhite, mask);
            
            // Find contours
            Cv2.FindContours(mask, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            
            var candidates = new List<(System.Drawing.Rectangle rect, double score)>();
            
            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                
                // Filter by captcha characteristics
                if (rect.Width >= 200 && rect.Width <= 400 && 
                    rect.Height >= 50 && rect.Height <= 100)
                {
                    // Check if it's in the center area (where captcha usually appears)
                    double centerX = (double)rect.X / screenshot.Width;
                    double centerY = (double)rect.Y / screenshot.Height;
                    
                    if (centerX >= 0.3 && centerX <= 0.7 && 
                        centerY >= 0.3 && centerY <= 0.7)
                    {
                        var confidence = CalculateColorConfidence(new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height), screenshot);
                        candidates.Add((new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height), confidence));
                    }
                }
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
        
        private double CalculateColorConfidence(System.Drawing.Rectangle region, Mat image)
        {
            double score = 0.0;
            
            // Size score (prefer captcha-like sizes)
            double sizeScore = 1.0 - Math.Abs(region.Width - 300) / 300.0;
            sizeScore = Math.Max(0, Math.Min(1, sizeScore));
            score += sizeScore * 0.4;
            
            // Aspect ratio score (prefer 3-5:1 ratio)
            double aspectRatio = (double)region.Width / region.Height;
            double aspectScore = 1.0 - Math.Abs(aspectRatio - 4.0) / 2.0;
            aspectScore = Math.Max(0, Math.Min(1, aspectScore));
            score += aspectScore * 0.3;
            
            // Position score (prefer center area)
            double centerX = (double)region.X / image.Width;
            double centerY = (double)region.Y / image.Height;
            double positionScore = 1.0 - Math.Abs(centerX - 0.5) / 0.5;
            positionScore = Math.Max(0, Math.Min(1, positionScore));
            score += positionScore * 0.3;
            
            return score * 100; // Convert to percentage
        }
        
        // Method to save current captcha as template
        public void SaveCaptchaTemplate(Mat screenshot, System.Drawing.Rectangle captchaRegion)
        {
            try
            {
                // Extract captcha region
                using var captchaROI = new Mat(screenshot, new OpenCvSharp.Rect(
                    captchaRegion.X, captchaRegion.Y, 
                    captchaRegion.Width, captchaRegion.Height));
                
                // Save full captcha window
                var windowPath = Path.Combine(_templatePath, "captcha_window.png");
                Cv2.ImWrite(windowPath, captchaROI);
                Console.WriteLine($"💾 Saved captcha window template: {windowPath}");
                
                // Extract just the white background area (inner part)
                var innerRect = new OpenCvSharp.Rect(
                    captchaRegion.Width / 8, captchaRegion.Height / 8,
                    captchaRegion.Width * 6 / 8, captchaRegion.Height * 6 / 8);
                
                if (innerRect.X >= 0 && innerRect.Y >= 0 && 
                    innerRect.X + innerRect.Width <= captchaROI.Width &&
                    innerRect.Y + innerRect.Height <= captchaROI.Height)
                {
                    using var backgroundROI = new Mat(captchaROI, innerRect);
                    var backgroundPath = Path.Combine(_templatePath, "captcha_background.png");
                    Cv2.ImWrite(backgroundPath, backgroundROI);
                    Console.WriteLine($"💾 Saved captcha background template: {backgroundPath}");
                }
                
                // Extract border area
                var borderRect = new OpenCvSharp.Rect(0, 0, captchaRegion.Width, captchaRegion.Height);
                using var borderROI = new Mat(captchaROI, borderRect);
                var borderPath = Path.Combine(_templatePath, "captcha_border.png");
                Cv2.ImWrite(borderPath, borderROI);
                Console.WriteLine($"💾 Saved captcha border template: {borderPath}");
                
                // Reload templates
                LoadTemplates();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving captcha template: {ex.Message}");
            }
        }
        
        // Method to check template status
        public string GetTemplateStatus()
        {
            return _templateManager.GetTemplateInfo();
        }
        
        // Method to check if templates exist
        public bool HasTemplates()
        {
            return _templateManager.HasTemplates();
        }
        
        // Method to delete templates (for testing)
        public void DeleteTemplates()
        {
            _templateManager.DeleteTemplates();
            LoadTemplates(); // Reload after deletion
        }
        
        public void Dispose()
        {
            _captchaWindowTemplate?.Dispose();
            _captchaBackgroundTemplate?.Dispose();
            _captchaBorderTemplate?.Dispose();
        }
    }
}
