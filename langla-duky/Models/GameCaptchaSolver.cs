using OpenCvSharp;
using Tesseract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;

public class GameCaptchaSolver : IDisposable
{
    private TesseractEngine? _tesseractEngine;
    private readonly string _tessdataPath;
    private bool _disposed = false;
    private Action<string>? _logMessage;

    public GameCaptchaSolver(string tessdataPath = @"./tessdata", Action<string>? logMessage = null)
    {
        _tessdataPath = tessdataPath;
        _logMessage = logMessage;
        InitializeTesseract();
    }

    private void LogMessage(string message)
    {
        if (_logMessage != null)
        {
            _logMessage(message);
        }
        else
        {
            Console.WriteLine(message);
        }
    }

    private void InitializeTesseract()
    {
        try
        {
            _tesseractEngine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
            
            // Optimal settings for 4-character letter captcha
            _tesseractEngine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
            _tesseractEngine.SetVariable("user_defined_dpi", "300");
            _tesseractEngine.SetVariable("tessedit_pageseg_mode", "7"); // Single text line
            _tesseractEngine.SetVariable("tessedit_ocr_engine_mode", "0"); // Legacy + LSTM
            
            LogMessage("✅ Tesseract engine initialized successfully");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize Tesseract: {ex.Message}");
        }
    }

    /// <summary>
    /// Main method to solve captcha - handles both file path and byte array
    /// </summary>
    public CaptchaResult SolveCaptcha(string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"Image file not found: {imagePath}");

        using var mat = Cv2.ImRead(imagePath);
        return SolveCaptcha(mat);
    }

    public CaptchaResult SolveCaptcha(byte[] imageBytes)
    {
        using var mat = Mat.FromImageData(imageBytes);
        return SolveCaptcha(mat);
    }

    /// <summary>
    /// Core captcha solving logic for 4-character letter captcha
    /// </summary>
    public CaptchaResult SolveCaptcha(Mat inputImage)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CaptchaResult();

        try
        {
            LogMessage($"🔍 Processing captcha image: {inputImage.Size()}");

            // Step 1: Remove dark borders first
            using var cropped = RemoveDarkBordersByColor(inputImage);
            
            // Save debug cropped image
            var croppedDebugPath = Path.Combine("captcha_debug", $"cropped_after_border_removal_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(croppedDebugPath, cropped);
            LogMessage($"💾 Saved cropped image: {croppedDebugPath}");
            
            // Step 2: Optimize image size for OCR
            using var optimized = OptimizeImageForOCR(cropped);
            
            // Step 2: Multiple preprocessing approaches
            var approaches = new List<Func<Mat, PreprocessResult>>
            {
                ProcessAsBinary,
                ProcessWithOtsuThreshold,
                ProcessWithAdaptiveThreshold,
                ProcessWithMultipleThresholds,
                ProcessWithMorphology,
                ProcessWithInversion
            };

            var candidates = new List<CandidateResult>();

            // Try each preprocessing approach
            foreach (var approach in approaches)
            {
                try
                {
                    LogMessage($"🔍 Trying preprocessing approach: {approach.Method.Name}");
                    using var preprocessResult = approach(optimized);
                    if (preprocessResult.Success && preprocessResult.ProcessedImage != null)
                    {
                        LogMessage($"✅ Preprocessing successful: {preprocessResult.Method}");
                        var ocrResult = PerformOCR(preprocessResult.ProcessedImage);
                        LogMessage($"🔍 OCR result: '{ocrResult.Text}' (confidence: {ocrResult.Confidence:F1}%)");
                        
                        if (IsValidCaptchaResult(ocrResult.Text))
                        {
                            LogMessage($"✅ Valid captcha result: '{ocrResult.Text}'");
                            candidates.Add(new CandidateResult
                            {
                                Text = ocrResult.Text,
                                Confidence = ocrResult.Confidence,
                                Method = preprocessResult.Method
                            });
                        }
                        else
                        {
                            LogMessage($"❌ Invalid captcha result: '{ocrResult.Text}' (length: {ocrResult.Text.Length})");
                        }
                    }
                    else
                    {
                        LogMessage($"❌ Preprocessing failed: {preprocessResult.Method}");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"⚠️ Approach failed: {ex.Message}");
                }
            }

            // Select best result
            if (candidates.Any())
            {
                var bestCandidate = candidates.OrderByDescending(c => c.Confidence).First();
                
                result.Text = bestCandidate.Text.ToLower();
                result.Confidence = bestCandidate.Confidence;
                result.Success = true;
                result.Method = bestCandidate.Method;
                result.ProcessingTime = stopwatch.Elapsed;

                LogMessage($"✅ Captcha solved: '{result.Text}' (Confidence: {result.Confidence:F1}%, Method: {result.Method})");
            }
            else
            {
                result.Success = false;
                result.Error = "No valid captcha text detected";
                LogMessage("❌ Failed to solve captcha");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            LogMessage($"💥 Error: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Remove dark borders by color detection and crop to content area
    /// </summary>
    private Mat RemoveDarkBordersByColor(Mat input)
    {
        // Convert to HSV for better color detection
        using var hsv = new Mat();
        if (input.Channels() == 3)
        {
            Cv2.CvtColor(input, hsv, ColorConversionCodes.BGR2HSV);
        }
        else if (input.Channels() == 4)
        {
            using var bgr = new Mat();
            Cv2.CvtColor(input, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
        }
        else
        {
            return input.Clone();
        }

        // Define brown/dark color range (HSV) - adjusted for better detection
        var lowerBrown = new Scalar(0, 30, 10);    // Lower bound for brown/dark colors (lower saturation/value)
        var upperBrown = new Scalar(30, 255, 80);  // Upper bound for brown/dark colors (lower max value)
        
        // Create mask for dark/brown areas
        using var mask = new Mat();
        Cv2.InRange(hsv, lowerBrown, upperBrown, mask);
        
        // Invert mask to get content area
        using var contentMask = new Mat();
        Cv2.BitwiseNot(mask, contentMask);
        
        // Save debug masks
        var debugPath1 = Path.Combine("captcha_debug", $"dark_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        var debugPath2 = Path.Combine("captcha_debug", $"content_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Directory.CreateDirectory("captcha_debug");
        Cv2.ImWrite(debugPath1, mask);
        Cv2.ImWrite(debugPath2, contentMask);
        LogMessage($"💾 Saved dark mask: {debugPath1}");
        LogMessage($"💾 Saved content mask: {debugPath2}");
        
        // Find contours of content area
        var contours = Cv2.FindContoursAsMat(contentMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        LogMessage($"🔍 Found {contours.Length} content contours");
        
        if (contours.Length > 0)
        {
            // Find the largest contour (should be the main content area)
            var largestContour = contours[0];
            var maxArea = Cv2.ContourArea(largestContour);
            
            foreach (var contour in contours)
            {
                var area = Cv2.ContourArea(contour);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestContour = contour;
                }
            }
            
            var boundingRect = Cv2.BoundingRect(largestContour);
            LogMessage($"🔍 Content bounding rect: {boundingRect.X},{boundingRect.Y} {boundingRect.Width}x{boundingRect.Height}");
            
            // Add small padding
            var padding = 3;
            var x = Math.Max(0, boundingRect.X - padding);
            var y = Math.Max(0, boundingRect.Y - padding);
            var width = Math.Min(input.Width - x, boundingRect.Width + 2 * padding);
            var height = Math.Min(input.Height - y, boundingRect.Height + 2 * padding);
            
            // Only crop if there's significant difference
            if (width < input.Width - 10 || height < input.Height - 10)
            {
                var roi = new OpenCvSharp.Rect(x, y, width, height);
                var cropped = new Mat(input, roi);
                
                LogMessage($"🔲 Removed dark borders: {input.Width}x{input.Height} -> {width}x{height}");
                return cropped;
            }
            else
            {
                LogMessage("⚠️ Content area covers most of image, trying alternative border removal");
                // Fallback to alternative method
                return RemoveBordersByEdgeDetection(input);
            }
        }
        
        LogMessage("⚠️ No content area found, trying alternative border removal");
        return RemoveBordersByEdgeDetection(input);
    }

    /// <summary>
    /// Alternative border removal using edge detection
    /// </summary>
    private Mat RemoveBordersByEdgeDetection(Mat input)
    {
        // Convert to grayscale
        Mat gray;
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        // Apply Gaussian blur to reduce noise
        using var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

        // Apply Canny edge detection
        using var edges = new Mat();
        Cv2.Canny(blurred, edges, 50, 150);

        // Find contours
        var contours = Cv2.FindContoursAsMat(edges, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        LogMessage($"🔍 Edge detection found {contours.Length} contours");

        if (contours.Length > 0)
        {
            // Find the contour with the largest area that's not too close to the edges
            var bestContour = contours[0];
            var maxArea = 0.0;
            
            foreach (var contour in contours)
            {
                var area = Cv2.ContourArea(contour);
                var boundingRect = Cv2.BoundingRect(contour);
                
                // Skip contours too close to edges (likely borders)
                var margin = 5;
                if (boundingRect.X > margin && boundingRect.Y > margin &&
                    boundingRect.X + boundingRect.Width < input.Width - margin &&
                    boundingRect.Y + boundingRect.Height < input.Height - margin &&
                    area > maxArea)
                {
                    maxArea = area;
                    bestContour = contour;
                }
            }
            
            if (maxArea > 100) // Minimum area threshold
            {
                var boundingRect = Cv2.BoundingRect(bestContour);
                LogMessage($"🔍 Edge-based content rect: {boundingRect.X},{boundingRect.Y} {boundingRect.Width}x{boundingRect.Height}");
                
                // Add padding
                var padding = 5;
                var x = Math.Max(0, boundingRect.X - padding);
                var y = Math.Max(0, boundingRect.Y - padding);
                var width = Math.Min(input.Width - x, boundingRect.Width + 2 * padding);
                var height = Math.Min(input.Height - y, boundingRect.Height + 2 * padding);
                
                if (width < input.Width - 10 || height < input.Height - 10)
                {
                    var roi = new OpenCvSharp.Rect(x, y, width, height);
                    var cropped = new Mat(input, roi);
                    
                    LogMessage($"🔲 Edge-based border removal: {input.Width}x{input.Height} -> {width}x{height}");
                    gray.Dispose();
                    return cropped;
                }
            }
        }
        
        LogMessage("⚠️ Edge detection failed, returning original");
        gray.Dispose();
        return input.Clone();
    }

    /// <summary>
    /// Remove black borders and crop to content area (legacy method)
    /// </summary>
    private Mat RemoveBordersAndCrop(Mat input)
    {
        // Convert to grayscale if needed
        Mat gray;
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        // Find non-black pixels (content area) - use much higher threshold to detect dark borders
        using var mask = new Mat();
        Cv2.Threshold(gray, mask, 120, 255, ThresholdTypes.Binary); // Remove dark areas (0-120)
        
        // Save debug mask
        var debugPath = Path.Combine("captcha_debug", $"border_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Directory.CreateDirectory("captcha_debug");
        Cv2.ImWrite(debugPath, mask);
        LogMessage($"💾 Saved border mask: {debugPath}");
        
        // Find bounding rectangle of content
        var contours = Cv2.FindContoursAsMat(mask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        LogMessage($"🔍 Found {contours.Length} contours for border detection");
        
        if (contours.Length > 0)
        {
            var boundingRect = Cv2.BoundingRect(contours[0]);
            LogMessage($"🔍 Largest contour bounding rect: {boundingRect.X},{boundingRect.Y} {boundingRect.Width}x{boundingRect.Height}");
            
            // Add small padding
            var padding = 5;
            var x = Math.Max(0, boundingRect.X - padding);
            var y = Math.Max(0, boundingRect.Y - padding);
            var width = Math.Min(input.Width - x, boundingRect.Width + 2 * padding);
            var height = Math.Min(input.Height - y, boundingRect.Height + 2 * padding);
            
            // Only crop if there's significant difference
            if (width < input.Width - 10 || height < input.Height - 10)
            {
                var roi = new OpenCvSharp.Rect(x, y, width, height);
                var cropped = new Mat(input, roi);
                
                LogMessage($"🔲 Removed borders: {input.Width}x{input.Height} -> {width}x{height}");
                gray.Dispose();
                return cropped;
            }
            else
            {
                LogMessage("⚠️ Border detection found minimal difference, keeping original size");
            }
        }
        
        LogMessage("⚠️ No content found, returning original");
        gray.Dispose();
        return input.Clone();
    }

    /// <summary>
    /// Optimize image resolution for best OCR performance
    /// </summary>
    private Mat OptimizeImageForOCR(Mat input)
    {
        // Check if image is too large (performance) or too small (accuracy)
        var currentHeight = input.Height;
        
        // Target character height: 40-60 pixels for optimal OCR
        double scaleFactor = 1.0;

        if (currentHeight > 200) // Too large - resize down for speed
        {
            scaleFactor = 150.0 / currentHeight;
        }
        else if (currentHeight < 30) // Too small - resize up for accuracy
        {
            scaleFactor = 50.0 / currentHeight;
        }

        if (Math.Abs(scaleFactor - 1.0) > 0.1) // Only resize if significant change needed
        {
            var newSize = new OpenCvSharp.Size((int)(input.Width * scaleFactor), (int)(input.Height * scaleFactor));
            var resized = new Mat();
            Cv2.Resize(input, resized, newSize, interpolation: InterpolationFlags.Cubic);
            
            Console.WriteLine($"📏 Resized from {input.Size()} to {newSize} (factor: {scaleFactor:F2})");
            return resized;
        }

        return input.Clone();
    }

    /// <summary>
    /// Preprocessing Method 1: Direct binary processing (for already clean images)
    /// </summary>
    private PreprocessResult ProcessAsBinary(Mat input)
    {
        Mat gray;
        
        // Convert to grayscale if needed
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        // Check if already binary-like (save processing)
        var hist = new Mat();
        using var mask = new Mat();
        Cv2.CalcHist(new[] { gray }, new[] { 0 }, mask, hist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
        
        // Get histogram data
        var histData = new float[256];
        hist.GetArray(out histData);
        
        var whitePixels = histData.Skip(200).Sum();
        var blackPixels = histData.Take(50).Sum();
        var totalPixels = gray.Rows * gray.Cols;
        
        if ((whitePixels + blackPixels) / totalPixels > 0.85) // Already binary-like
        {
            LogMessage("✅ Image already binary, using direct processing");
            var result = new PreprocessResult { ProcessedImage = gray.Clone(), Success = true, Method = "Direct Binary" };
            gray.Dispose();
            return result;
        }

        // If not binary-like, apply threshold
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 127, 255, ThresholdTypes.Binary);
        
        var result2 = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "Direct Binary" };
        gray.Dispose();
        return result2;
    }

    /// <summary>
    /// Preprocessing Method 2: Otsu threshold (automatic threshold selection)
    /// </summary>
    private PreprocessResult ProcessWithOtsuThreshold(Mat input)
    {
        Mat gray;
        
        // Convert to grayscale if needed
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        using var binary = new Mat();
        var threshold = Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        LogMessage($"🎯 Otsu threshold: {threshold}");

        // Optional cleanup for noisy images
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
        using var cleaned = new Mat();
        Cv2.MorphologyEx(binary, cleaned, MorphTypes.Close, kernel);

        var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Otsu Threshold" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Preprocessing Method 3: Adaptive threshold (for uneven lighting)
    /// </summary>
    private PreprocessResult ProcessWithAdaptiveThreshold(Mat input)
    {
        Mat gray;
        
        // Convert to grayscale if needed
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        using var adaptive = new Mat();
        Cv2.AdaptiveThreshold(gray, adaptive, 255, AdaptiveThresholdTypes.GaussianC, 
                            ThresholdTypes.Binary, 11, 2);

        var result = new PreprocessResult { ProcessedImage = adaptive.Clone(), Success = true, Method = "Adaptive Threshold" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Preprocessing Method 4: Try multiple fixed thresholds and pick best
    /// </summary>
    private PreprocessResult ProcessWithMultipleThresholds(Mat input)
    {
        Mat gray;
        
        // Convert to grayscale if needed
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        var thresholds = new[] { 100, 127, 150, 180, 200 };
        var bestResult = "";
        var bestConfidence = 0f;
        Mat? bestImage = null;

        foreach (var thresh in thresholds)
        {
            using var binary = new Mat();
            Cv2.Threshold(gray, binary, thresh, 255, ThresholdTypes.Binary);

            // Quick OCR test
            var testResult = PerformOCR(binary);
            if (testResult.Confidence > bestConfidence && IsValidCaptchaResult(testResult.Text))
            {
                bestConfidence = testResult.Confidence;
                bestResult = testResult.Text;
                bestImage?.Dispose();
                bestImage = binary.Clone();
            }
        }

        if (bestImage != null)
        {
            LogMessage($"🎯 Best threshold result: '{bestResult}' (confidence: {bestConfidence:F1}%)");
            var result = new PreprocessResult { ProcessedImage = bestImage, Success = true, Method = "Multi-Threshold" };
            gray.Dispose();
            return result;
        }

        gray.Dispose();
        return new PreprocessResult { Success = false };
    }

    /// <summary>
    /// Preprocessing Method 5: Morphological operations for better character separation
    /// </summary>
    private PreprocessResult ProcessWithMorphology(Mat input)
    {
        Mat gray;
        
        // Convert to grayscale if needed
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        // Apply Otsu threshold first
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        // Morphological operations to clean up characters
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
        
        // Close small gaps in characters
        using var closed = new Mat();
        Cv2.MorphologyEx(binary, closed, MorphTypes.Close, kernel);
        
        // Open to separate touching characters
        using var opened = new Mat();
        Cv2.MorphologyEx(closed, opened, MorphTypes.Open, kernel);
        
        // Final cleanup
        using var cleaned = new Mat();
        Cv2.MorphologyEx(opened, cleaned, MorphTypes.Close, kernel);

        var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Morphology" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Preprocessing Method 6: Invert colors for better OCR
    /// </summary>
    private PreprocessResult ProcessWithInversion(Mat input)
    {
        Mat gray;
        
        // Convert to grayscale if needed
        if (input.Channels() == 3)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }
        else
        {
            gray = input.Clone();
        }

        // Apply Otsu threshold first
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        // Invert colors (black text on white background -> white text on black background)
        using var inverted = new Mat();
        Cv2.BitwiseNot(binary, inverted);

        var result = new PreprocessResult { ProcessedImage = inverted.Clone(), Success = true, Method = "Inversion" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Perform OCR using Tesseract
    /// </summary>
    private OCRResult PerformOCR(Mat processedImage)
    {
        try
        {
            if (_tesseractEngine == null)
            {
                LogMessage("❌ Tesseract engine is null");
                return new OCRResult { Success = false };
            }

            LogMessage($"🔍 OCR Input: {processedImage.Width}x{processedImage.Height}, channels: {processedImage.Channels()}, type: {processedImage.Type()}");

            // Save debug image
            var debugPath = Path.Combine("captcha_debug", $"tesseract_input_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Directory.CreateDirectory("captcha_debug");
            Cv2.ImWrite(debugPath, processedImage);
            LogMessage($"💾 Saved Tesseract input: {debugPath}");

            using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(processedImage);
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            using var pix = Pix.LoadFromMemory(memoryStream.ToArray());
            
            // Try multiple PSM modes for better results
            var psmModes = new[] { PageSegMode.SingleWord, PageSegMode.SingleLine, PageSegMode.SingleBlock, PageSegMode.RawLine };
            var bestResult = "";
            var bestConfidence = 0f;
            
            foreach (var psmMode in psmModes)
            {
                using var page = _tesseractEngine.Process(pix, psmMode);
                var text = page.GetText()?.Trim() ?? "";
                var confidence = page.GetMeanConfidence();
                
                LogMessage($"🔍 PSM {psmMode}: '{text}' (confidence: {confidence:F1}%)");
                
                if (confidence > bestConfidence && !string.IsNullOrEmpty(text))
                {
                    bestConfidence = confidence;
                    bestResult = text;
                }
            }
            
            LogMessage($"🔍 Best OCR result: '{bestResult}' (confidence: {bestConfidence:F1}%)");

            return new OCRResult
            {
                Text = bestResult,
                Confidence = bestConfidence,
                Success = !string.IsNullOrEmpty(bestResult)
            };
        }
        catch (Exception ex)
        {
            LogMessage($"🔥 OCR Error: {ex.Message}");
            return new OCRResult { Success = false };
        }
    }

    /// <summary>
    /// Validate if OCR result looks like a valid 4-character letter captcha
    /// </summary>
    private bool IsValidCaptchaResult(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim().ToLower();

        // Check length (captcha has exactly 4 characters)
        if (text.Length != 4)
            return false;

        // Check if contains only letters (no numbers)
        if (!text.All(c => char.IsLetter(c)))
            return false;

        return true;
    }

    /// <summary>
    /// Batch process multiple captchas
    /// </summary>
    public List<CaptchaResult> SolveCaptchasBatch(string[] imagePaths)
    {
        var results = new List<CaptchaResult>();
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"🔄 Processing {imagePaths.Length} captchas...");

        foreach (var path in imagePaths)
        {
            try
            {
                var result = SolveCaptcha(path);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(new CaptchaResult 
                { 
                    Success = false, 
                    Error = ex.Message,
                    ProcessingTime = TimeSpan.Zero
                });
            }
        }

        stopwatch.Stop();
        
        var successCount = results.Count(r => r.Success);
        var successRate = (double)successCount / results.Count * 100;
        
        Console.WriteLine($"📊 Batch completed: {successCount}/{results.Count} ({successRate:F1}%) in {stopwatch.Elapsed.TotalSeconds:F1}s");

        return results;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _tesseractEngine?.Dispose();
            _disposed = true;
        }
    }
}

// Supporting classes
public class CaptchaResult
{
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public bool Success { get; set; }
    public string Method { get; set; } = "";
    public string Error { get; set; } = "";
    public TimeSpan ProcessingTime { get; set; }
}

public class PreprocessResult : IDisposable
{
    public Mat? ProcessedImage { get; set; }
    public bool Success { get; set; }
    public string Method { get; set; } = "";

    public void Dispose()
    {
        ProcessedImage?.Dispose();
    }
}

public class OCRResult
{
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public bool Success { get; set; }
}

public class CandidateResult
{
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public string Method { get; set; } = "";
}
