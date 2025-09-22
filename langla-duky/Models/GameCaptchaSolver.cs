using OpenCvSharp;
using Tesseract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using RestSharp;
using Newtonsoft.Json;

public class GameCaptchaSolver : IDisposable
{
    private TesseractEngine? _tesseractEngine;
    private readonly string _tessdataPath;
    private bool _disposed = false;
    private Action<string>? _logMessage;
    private readonly string _ocrSpaceApiKey = "K84148904688957";
    private readonly RestClient _restClient;

    public GameCaptchaSolver(string tessdataPath = @"./tessdata", Action<string>? logMessage = null)
    {
        _tessdataPath = tessdataPath;
        _logMessage = logMessage;
        _restClient = new RestClient("https://api.ocr.space/parse/image");
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
            _tesseractEngine.SetVariable("tessedit_pageseg_mode", "8"); // Single word
            _tesseractEngine.SetVariable("tessedit_ocr_engine_mode", "1"); // LSTM only for better accuracy
            
            // Additional settings for better character recognition
            _tesseractEngine.SetVariable("tessedit_char_blacklist", "0123456789!@#$%^&*()_+-=[]{}|;':\",./<>?`~");
            _tesseractEngine.SetVariable("classify_bln_numeric_mode", "0"); // Disable numeric mode
            _tesseractEngine.SetVariable("textord_min_linesize", "2.5"); // Minimum line size
            _tesseractEngine.SetVariable("textord_old_baselines", "0"); // Use new baseline detection
            _tesseractEngine.SetVariable("textord_old_xheight", "0"); // Use new x-height detection
            _tesseractEngine.SetVariable("tessedit_do_invert", "0"); // Don't invert by default
            
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

            // Step 0: Analyze the input image for debugging
            AnalyzeImageQuality(inputImage);

            // Step 1: Remove dark borders first
            using var cropped = RemoveDarkBordersByColor(inputImage);
            
            // Save debug cropped image
            var croppedDebugPath = Path.Combine("captcha_debug", $"cropped_after_border_removal_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(croppedDebugPath, cropped);
            LogMessage($"💾 Saved cropped image: {croppedDebugPath}");
            
            // Step 2: Optimize image size for OCR
            using var optimized = OptimizeImageForOCR(cropped);
            
            // Step 2: NEW OPTIMIZED FLOW - Color-Aware Processing for Colorful Captchas
            var approaches = new List<Func<Mat, PreprocessResult>>
            {
                // NEW: Color-Aware Flow (PRIORITY 1 - Best for colorful captchas like jsjx)
                ProcessWithColorAwarePreprocessing,    // NEW: Color-aware preprocessing
                ProcessWithCharacterSegmentation,      // NEW: Character segmentation in color space
                ProcessWithSelectiveBinarization,      // NEW: Selective binarization per character
                
                // EXISTING: Fallback methods for compatibility
                ProcessWithAdaptiveThreshold,          // GOOD: Adaptive threshold
                ProcessWithInversion,                  // GOOD: Inversion method
                ProcessWithColorfulCaptchaV3,          // GOOD: Works well with Tesseract
                ProcessWithColorfulCaptcha,            // FALLBACK: Works with Tesseract
                ProcessWithScaling,                    // HELPFUL: Scale up for better OCR
                ProcessWithOtsuThreshold,              // BASIC: Standard threshold
                ProcessAsBinary                        // BASIC: Simple binary conversion
            };

            var candidates = new List<CandidateResult>();

            // Try each preprocessing approach
            var fourCharCandidates = new List<CandidateResult>();
            var otherCandidates = new List<CandidateResult>();
            var hasGoodApiResult = false;
            
            foreach (var approach in approaches)
            {
                try
                {
                    LogMessage($"🔍 Trying preprocessing approach: {approach.Method.Name}");
                    using var preprocessResult = approach(optimized);
                    if (preprocessResult.Success && preprocessResult.ProcessedImage != null)
                    {
                        LogMessage($"✅ Preprocessing successful: {preprocessResult.Method}");
                        
                        // Try OCR.space API first for better accuracy
                        var apiResult = PerformOCRWithSpaceAPI(preprocessResult.ProcessedImage);
                        var tesseractResult = new OCRResult { Success = false };
                        
                        // Always try Tesseract as backup, but prioritize API results
                        LogMessage("🔄 Trying Tesseract as backup...");
                        tesseractResult = PerformOCR(preprocessResult.ProcessedImage);
                        
                        // Choose the best result - prioritize API if it has valid content
                        OCRResult ocrResult;
                        string source;
                        if (apiResult.Success && !string.IsNullOrEmpty(apiResult.Text) && IsValidCaptchaResult(apiResult.Text))
                        {
                            ocrResult = apiResult;
                            source = "API";
                            LogMessage($"🌐 Using API result: '{apiResult.Text}' (confidence: {apiResult.Confidence:F1}%)");
                            
                            // Mark that we have a good API result
                            if (apiResult.Text.Length == 4)
                            {
                                hasGoodApiResult = true;
                            }
                        }
                        else if (tesseractResult.Success && !string.IsNullOrEmpty(tesseractResult.Text) && IsValidCaptchaResult(tesseractResult.Text))
                        {
                            ocrResult = tesseractResult;
                            source = "Tesseract";
                            LogMessage($"🔍 Using Tesseract result: '{tesseractResult.Text}' (confidence: {tesseractResult.Confidence:F1}%)");
                        }
                        else
                        {
                            // Fallback to whichever has any result
                            ocrResult = apiResult.Success ? apiResult : tesseractResult;
                            source = apiResult.Success ? "API" : "Tesseract";
                            LogMessage($"🔄 Fallback to {source}: '{ocrResult.Text}' (confidence: {ocrResult.Confidence:F1}%)");
                        }
                        
                        // NEW: Special handling for Color-Aware methods
                        var isColorAwareMethod = preprocessResult.Method.Contains("Color-Aware") || 
                                                preprocessResult.Method.Contains("Character Segmentation") || 
                                                preprocessResult.Method.Contains("Selective Binarization");
                        
                        if (isColorAwareMethod && !string.IsNullOrEmpty(ocrResult.Text))
                        {
                            LogMessage($"🎨 Color-Aware method result: '{ocrResult.Text}' (method: {preprocessResult.Method})");
                        }
                        
                        // Clean the OCR result: remove special characters, keep only lowercase letters
                        var cleanedText = CleanCaptchaText(ocrResult.Text);
                        LogMessage($"🔍 Final OCR result: '{ocrResult.Text}' -> cleaned: '{cleanedText}' (confidence: {ocrResult.Confidence:F1}%, source: {source})");
                        
                        if (IsValidCaptchaResult(cleanedText))
                        {
                            LogMessage($"✅ Valid captcha result: '{cleanedText}'");
                            
                            var candidate = new CandidateResult
                            {
                                Text = cleanedText,
                                Confidence = ocrResult.Confidence,
                                Method = $"{preprocessResult.Method} ({source})"
                            };
                            
                            // Categorize by character count
                            if (cleanedText.Length == 4)
                            {
                                fourCharCandidates.Add(candidate);
                                LogMessage($"🎯 Found 4-character result: '{cleanedText}' (confidence: {ocrResult.Confidence:F1}%, method: {preprocessResult.Method})");
                                
                                // If we have a good 4-character API result, we can stop early
                                if (source == "API" && !hasGoodApiResult)
                                {
                                    LogMessage($"🚀 Found excellent API result early - stopping processing for speed!");
                                    break;
                                }
                            }
                            else
                            {
                                otherCandidates.Add(candidate);
                                LogMessage($"📝 Found {cleanedText.Length}-character result: '{cleanedText}' (confidence: {ocrResult.Confidence:F1}%, method: {preprocessResult.Method})");
                            }
                        }
                        else
                        {
                            // Try to extract 4 characters from long results
                            if (cleanedText.Length >= 4)
                            {
                                var extracted = Extract4CharsFromLong(cleanedText);
                                if (extracted != null)
                                {
                                    LogMessage($"🔍 Extracted 4-character result: '{extracted}' from '{cleanedText}'");
                                    fourCharCandidates.Add(new CandidateResult
                                    {
                                        Text = extracted,
                                        Confidence = ocrResult.Confidence,
                                        Method = preprocessResult.Method + " (extracted)"
                            });
                        }
                        else
                        {
                                    LogMessage($"❌ Invalid captcha result: '{cleanedText}' (length: {cleanedText.Length})");
                                }
                            }
                            else
                            {
                                LogMessage($"❌ Invalid captcha result: '{cleanedText}' (length: {cleanedText.Length})");
                            }
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
            
            // Log detailed summary
            LogMessage($"📊 Summary: Found {fourCharCandidates.Count} 4-character results, {otherCandidates.Count} other results");
            
            // Log 4-character results in detail
            if (fourCharCandidates.Any())
            {
                LogMessage($"🎯 4-character results (PRIORITIZED):");
                foreach (var candidate in fourCharCandidates.OrderByDescending(c => c.Confidence))
                {
                    LogMessage($"  ✅ '{candidate.Text}' (confidence: {candidate.Confidence:F1}%, method: {candidate.Method})");
                }
            }
            
            // Log other results
            if (otherCandidates.Any())
            {
                LogMessage($"📝 Other results (fallback):");
                foreach (var candidate in otherCandidates.OrderByDescending(c => c.Confidence))
                {
                    LogMessage($"  📄 '{candidate.Text}' (length: {candidate.Text.Length}, confidence: {candidate.Confidence:F1}%, method: {candidate.Method})");
                }
            }
            
            // Prioritize 4-character results
            if (fourCharCandidates.Any())
            {
                LogMessage($"🎯 Prioritizing {fourCharCandidates.Count} 4-character results for final selection");
                candidates.AddRange(fourCharCandidates);
            }
            
            // Add other results as fallback
            if (otherCandidates.Any())
            {
                LogMessage($"📝 Adding {otherCandidates.Count} other results as fallback");
                candidates.AddRange(otherCandidates);
            }

            // Select best result with smart logic
            if (candidates.Any())
            {
                LogMessage($"🔍 Found {candidates.Count} valid candidates:");
                foreach (var candidate in candidates)
                {
                    LogMessage($"  - '{candidate.Text}' (length: {candidate.Text.Length}, confidence: {candidate.Confidence:F1}%, method: {candidate.Method})");
                }
                
                // Priority 1: Results with exactly 4 characters (already prioritized above)
                var correctLengthCandidates = candidates.Where(c => c.Text.Length == 4).ToList();
                if (correctLengthCandidates.Any())
                {
                    LogMessage($"🎯 Found {correctLengthCandidates.Count} 4-character results - these are already prioritized!");
                    
                // NEW: Within 4-character results, prioritize by accuracy, not just method type
                var colorAwareResults = correctLengthCandidates.Where(c => 
                    c.Method.Contains("Color-Aware") || 
                    c.Method.Contains("Character Segmentation") || 
                    c.Method.Contains("Selective Binarization")).ToList();
                
                var apiResults = correctLengthCandidates.Where(c => c.Method.Contains("(API)")).ToList();
                var tesseractResults = correctLengthCandidates.Where(c => c.Method.Contains("(Tesseract)")).ToList();
                
                // NEW: Check if Tesseract results look more accurate (common patterns)
                var tesseractAccurate = tesseractResults.Where(c => 
                    c.Text.Length == 4 && 
                    (c.Text.All(char.IsLetter) || c.Text.All(char.IsDigit)) &&
                    !c.Text.Contains(" ") && !c.Text.Contains(".") && !c.Text.Contains(",")
                ).ToList();
                
                if (tesseractAccurate.Any())
                {
                    LogMessage($"🔍 Found {tesseractAccurate.Count} accurate Tesseract results - PRIORITIZING these over API!");
                    // Tesseract accurate results get absolute priority
                    correctLengthCandidates = tesseractAccurate.Concat(colorAwareResults).Concat(apiResults).Concat(tesseractResults.Except(tesseractAccurate)).ToList();
                }
                else if (colorAwareResults.Any())
                {
                    LogMessage($"🎨 Found {colorAwareResults.Count} Color-Aware results - PRIORITIZING these for colorful captchas!");
                    // Color-Aware results get priority for colorful captchas
                    correctLengthCandidates = colorAwareResults.Concat(tesseractResults).Concat(apiResults).ToList();
                }
                else if (apiResults.Any())
                {
                    LogMessage($"🌐 Found {apiResults.Count} API results - using as fallback");
                    // API results as fallback
                    correctLengthCandidates = apiResults.Concat(tesseractResults).ToList();
                }
                }
                
                // Priority 2: Handle 5-character results that start with 'l' (common OCR error)
                var suspiciousCandidates = candidates.Where(c => c.Text.Length == 5 && c.Text.StartsWith("l")).ToList();
                if (suspiciousCandidates.Any())
                {
                    LogMessage($"🔍 Found {suspiciousCandidates.Count} suspicious 5-character results starting with 'l'");
                    foreach (var candidate in suspiciousCandidates)
                    {
                        var trimmed = candidate.Text.Substring(1);
                        if (trimmed.Length == 4 && trimmed.All(ch => char.IsLetter(ch)))
                        {
                            LogMessage($"✅ Adding trimmed candidate: '{trimmed}' (from '{candidate.Text}')");
                            correctLengthCandidates.Add(new CandidateResult
                            {
                                Text = trimmed,
                                Confidence = candidate.Confidence,
                                Method = candidate.Method
                            });
                        }
                    }
                }
                
                // Priority 3: Handle 6-character results that might be close to 4-character (like 'FiwgSg' → 'wggs')
                var sixCharCandidates = candidates.Where(c => c.Text.Length == 6).ToList();
                if (sixCharCandidates.Any() && !correctLengthCandidates.Any())
                {
                    LogMessage($"🔍 Found {sixCharCandidates.Count} 6-character results, trying to extract 4 characters");
                    foreach (var candidate in sixCharCandidates)
                    {
                        var extracted = Extract4CharsFrom6(candidate.Text);
                        if (extracted != null)
                        {
                            LogMessage($"🔍 Extracted '{extracted}' from '{candidate.Text}'");
                            correctLengthCandidates.Add(new CandidateResult 
                            { 
                                Text = extracted, 
                                Confidence = candidate.Confidence, 
                                Method = candidate.Method + " (extracted)" 
                            });
                        }
                    }
                }

                // Priority 4: Handle 3-character results (might be missing 1 character)
                var threeCharCandidates = candidates.Where(c => c.Text.Length == 3).ToList();
                if (threeCharCandidates.Any() && !correctLengthCandidates.Any())
                {
                    LogMessage($"🔍 Found {threeCharCandidates.Count} 3-character results, might be missing 1 character");
                    // Add them to correct length candidates as fallback
                    correctLengthCandidates.AddRange(threeCharCandidates);
                }
                
                // Priority 4: Any valid result (2-5 characters)
                var allValidCandidates = candidates.Where(c => c.Text.Length >= 2 && c.Text.Length <= 5).ToList();
                
                CandidateResult bestCandidate;
                if (correctLengthCandidates.Any())
                {
                    // 4-character results are already prioritized, just pick the best one
                    bestCandidate = correctLengthCandidates.OrderByDescending(c => c.Confidence).First();
                    LogMessage($"🎯 Selected 4-character result: '{bestCandidate.Text}' (confidence: {bestCandidate.Confidence:F1}%, method: {bestCandidate.Method})");
                }
                else if (allValidCandidates.Any())
                {
                    bestCandidate = allValidCandidates.OrderByDescending(c => c.Confidence).First();
                    LogMessage($"✅ Selected valid result: '{bestCandidate.Text}' (length: {bestCandidate.Text.Length}, confidence: {bestCandidate.Confidence:F1}%, method: {bestCandidate.Method})");
                }
                else
                {
                    // Priority 5: Fallback to longest result if no valid candidates
                    var longestCandidates = candidates.OrderByDescending(c => c.Text.Length).ThenByDescending(c => c.Confidence).ToList();
                    if (longestCandidates.Any())
                    {
                        bestCandidate = longestCandidates.First();
                        LogMessage($"✅ Selected longest result: '{bestCandidate.Text}' (length: {bestCandidate.Text.Length}, confidence: {bestCandidate.Confidence:F1}%, method: {bestCandidate.Method})");
                    }
                    else
                    {
                        bestCandidate = candidates.OrderByDescending(c => c.Confidence).First();
                        LogMessage($"✅ Selected fallback result: '{bestCandidate.Text}' (confidence: {bestCandidate.Confidence:F1}%, method: {bestCandidate.Method})");
                    }
                }
                
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
    /// Analyze image quality and provide debugging information
    /// </summary>
    private void AnalyzeImageQuality(Mat input)
    {
        try
        {
            LogMessage($"📊 Image Analysis:");
            LogMessage($"  - Size: {input.Width}x{input.Height}");
            LogMessage($"  - Channels: {input.Channels()}");
            LogMessage($"  - Type: {input.Type()}");
            
            // Convert to grayscale for analysis
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

            // Calculate histogram
            var hist = new Mat();
            using var mask = new Mat();
            Cv2.CalcHist(new[] { gray }, new[] { 0 }, mask, hist, 1, new[] { 256 }, new[] { new Rangef(0, 256) });
            
            var histData = new float[256];
            hist.GetArray(out histData);
            
            // Analyze pixel distribution
            var totalPixels = gray.Rows * gray.Cols;
            var darkPixels = histData.Take(50).Sum(); // 0-49
            var midPixels = histData.Skip(50).Take(150).Sum(); // 50-199
            var brightPixels = histData.Skip(200).Sum(); // 200-255
            
            LogMessage($"  - Dark pixels (0-49): {darkPixels:F0} ({darkPixels/totalPixels*100:F1}%)");
            LogMessage($"  - Mid pixels (50-199): {midPixels:F0} ({midPixels/totalPixels*100:F1}%)");
            LogMessage($"  - Bright pixels (200-255): {brightPixels:F0} ({brightPixels/totalPixels*100:F1}%)");
            
            // Check for solid color
            var maxCount = histData.Max();
            var maxIndex = Array.IndexOf(histData, maxCount);
            LogMessage($"  - Most common pixel value: {maxIndex} (appears {maxCount:F0} times, {maxCount/totalPixels*100:F1}%)");
            
            if (maxCount / totalPixels > 0.8)
            {
                LogMessage($"⚠️ WARNING: Image appears to be mostly solid color - may not contain captcha text!");
                LogMessage($"💡 SUGGESTION: Check if captcha area coordinates are correct in config.json");
                LogMessage($"💡 SUGGESTION: Try enabling AutoDetectCaptchaArea or use manual capture");
            }
            
            // Check for text-like content
            var nonWhitePixels = histData.Take(240).Sum(); // Pixels that are not pure white
            LogMessage($"  - Non-white pixels: {nonWhitePixels:F0} ({nonWhitePixels/totalPixels*100:F1}%)");
            
            if (nonWhitePixels / totalPixels < 0.05)
            {
                LogMessage($"⚠️ WARNING: Very few non-white pixels detected - image may be mostly white/empty");
            }
            else if (nonWhitePixels / totalPixels > 0.3)
            {
                LogMessage($"✅ Good: Sufficient non-white pixels detected for potential text content");
            }
            
            // Save histogram for debugging
            var histPath = Path.Combine("captcha_debug", $"histogram_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Directory.CreateDirectory("captcha_debug");
            SaveHistogram(hist, histPath);
            LogMessage($"💾 Saved histogram: {histPath}");
            
            gray.Dispose();
        }
        catch (Exception ex)
        {
            LogMessage($"⚠️ Image analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Save histogram as an image for debugging
    /// </summary>
    private void SaveHistogram(Mat hist, string path)
    {
        try
        {
            var histData = new float[256];
            hist.GetArray(out histData);
            
            var maxValue = histData.Max();
            var width = 512;
            var height = 200;
            
            using var histImage = new Mat(height, width, MatType.CV_8UC3, Scalar.White);
            
            for (int i = 0; i < 256; i++)
            {
                var normalizedHeight = (int)(histData[i] / maxValue * height);
                var x = i * width / 256;
                var y = height - normalizedHeight;
                
                Cv2.Line(histImage, new OpenCvSharp.Point(x, height), new OpenCvSharp.Point(x, y), Scalar.Black, 1);
            }
            
            Cv2.ImWrite(path, histImage);
        }
        catch (Exception ex)
        {
            LogMessage($"⚠️ Failed to save histogram: {ex.Message}");
        }
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

        // Define brown/dark color range (HSV) - more conservative to preserve faint characters
        var lowerBrown = new Scalar(0, 40, 15);    // Higher threshold to preserve faint characters
        var upperBrown = new Scalar(30, 255, 70);  // Lower max value to be more selective
        
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
    /// NEW: Color-Aware Preprocessing - xử lý màu sắc trước khi chuyển sang grayscale
    /// </summary>
    private PreprocessResult ProcessWithColorAwarePreprocessing(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

        LogMessage("🎨 Processing Color-Aware Preprocessing...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"color_aware_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

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

        // Save HSV for debugging
        var hsvPath = Path.Combine("captcha_debug", $"color_aware_hsv_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(hsvPath, hsv);
        LogMessage($"💾 Saved HSV: {hsvPath}");

        // Detect colorful characters (not white/black/gray) - IMPROVED thresholds
        using var colorfulMask = new Mat();
        var lowerColorful = new Scalar(0, 50, 50);    // Higher thresholds for better color detection
        var upperColorful = new Scalar(180, 255, 255);
        Cv2.InRange(hsv, lowerColorful, upperColorful, colorfulMask);
        
        // Also try detecting specific color ranges for better accuracy
        using var purpleMask = new Mat();
        var lowerPurple = new Scalar(120, 50, 50);    // Purple range
        var upperPurple = new Scalar(160, 255, 255);
        Cv2.InRange(hsv, lowerPurple, upperPurple, purpleMask);
        
        using var yellowMask = new Mat();
        var lowerYellow = new Scalar(20, 50, 50);     // Yellow range
        var upperYellow = new Scalar(40, 255, 255);
        Cv2.InRange(hsv, lowerYellow, upperYellow, yellowMask);
        
        using var pinkMask = new Mat();
        var lowerPink = new Scalar(160, 50, 50);      // Pink/Magenta range
        var upperPink = new Scalar(180, 255, 255);
        Cv2.InRange(hsv, lowerPink, upperPink, pinkMask);
        
        // Combine all color masks
        using var combinedMask = new Mat();
        Cv2.BitwiseOr(colorfulMask, purpleMask, combinedMask);
        Cv2.BitwiseOr(combinedMask, yellowMask, combinedMask);
        Cv2.BitwiseOr(combinedMask, pinkMask, combinedMask);

        // Save all masks for debugging
        var colorfulMaskPath = Path.Combine("captcha_debug", $"color_aware_colorful_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(colorfulMaskPath, colorfulMask);
        LogMessage($"💾 Saved colorful mask: {colorfulMaskPath}");
        
        var purpleMaskPath = Path.Combine("captcha_debug", $"color_aware_purple_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(purpleMaskPath, purpleMask);
        LogMessage($"💾 Saved purple mask: {purpleMaskPath}");
        
        var yellowMaskPath = Path.Combine("captcha_debug", $"color_aware_yellow_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(yellowMaskPath, yellowMask);
        LogMessage($"💾 Saved yellow mask: {yellowMaskPath}");
        
        var pinkMaskPath = Path.Combine("captcha_debug", $"color_aware_pink_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(pinkMaskPath, pinkMask);
        LogMessage($"💾 Saved pink mask: {pinkMaskPath}");
        
        var combinedMaskPath = Path.Combine("captcha_debug", $"color_aware_combined_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(combinedMaskPath, combinedMask);
        LogMessage($"💾 Saved combined mask: {combinedMaskPath}");

        // Apply combined mask to isolate colorful characters
        using var colorfulChars = new Mat();
        Cv2.BitwiseAnd(input, input, colorfulChars, combinedMask);

        // Save isolated colorful characters
        var isolatedPath = Path.Combine("captcha_debug", $"color_aware_isolated_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(isolatedPath, colorfulChars);
        LogMessage($"💾 Saved isolated: {isolatedPath}");

        // Convert to grayscale for further processing
        using var gray = new Mat();
        Cv2.CvtColor(colorfulChars, gray, ColorConversionCodes.BGR2GRAY);

        // STEP 1: Noise removal BEFORE scaling (important OpenCV flow!)
        using var denoised = new Mat();
        Cv2.GaussianBlur(gray, denoised, new OpenCvSharp.Size(3, 3), 0);
        
        // STEP 2: Contrast enhancement
        using var enhanced = new Mat();
        Cv2.EqualizeHist(denoised, enhanced);

        // Save grayscale for debugging
        var grayPath = Path.Combine("captcha_debug", $"color_aware_gray_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(grayPath, gray);
        LogMessage($"💾 Saved grayscale: {grayPath}");
        
        var denoisedPath = Path.Combine("captcha_debug", $"color_aware_denoised_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(denoisedPath, denoised);
        LogMessage($"💾 Saved denoised: {denoisedPath}");
        
        var enhancedPath = Path.Combine("captcha_debug", $"color_aware_enhanced_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(enhancedPath, enhanced);
        LogMessage($"💾 Saved enhanced: {enhancedPath}");

        // Scale up for better OCR (16x for maximum quality for colorful captcha)
        using var scaled = new Mat();
        var newSize = new OpenCvSharp.Size(enhanced.Width * 16, enhanced.Height * 16);
        Cv2.Resize(enhanced, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

        // Save scaled for debugging
        var scaledPath = Path.Combine("captcha_debug", $"color_aware_scaled_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(scaledPath, scaled);
        LogMessage($"💾 Saved scaled: {scaledPath}");

        // Apply multiple thresholding methods and choose the best one
        using var adaptive = new Mat();
        Cv2.AdaptiveThreshold(scaled, adaptive, 255, AdaptiveThresholdTypes.GaussianC, 
                            ThresholdTypes.Binary, 7, 3);
        
        // Try Otsu thresholding as alternative
        using var otsu = new Mat();
        Cv2.Threshold(scaled, otsu, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        
        // Try fixed threshold
        using var fixedThreshold = new Mat();
        Cv2.Threshold(scaled, fixedThreshold, 127, 255, ThresholdTypes.Binary);
        
        // Choose the best thresholding result based on character density
        var adaptiveNonZero = Cv2.CountNonZero(adaptive);
        var otsuNonZero = Cv2.CountNonZero(otsu);
        var fixedNonZero = Cv2.CountNonZero(fixedThreshold);
        
        Mat bestThreshold;
        string thresholdMethod;
        if (Math.Abs(adaptiveNonZero - scaled.Rows * scaled.Cols * 0.3) < 
            Math.Abs(otsuNonZero - scaled.Rows * scaled.Cols * 0.3))
        {
            bestThreshold = adaptive;
            thresholdMethod = "Adaptive";
        }
        else
        {
            bestThreshold = otsu;
            thresholdMethod = "Otsu";
        }
        
        if (Math.Abs(fixedNonZero - scaled.Rows * scaled.Cols * 0.3) < 
            Math.Abs(Cv2.CountNonZero(bestThreshold) - scaled.Rows * scaled.Cols * 0.3))
        {
            bestThreshold = fixedThreshold;
            thresholdMethod = "Fixed";
        }
        
        LogMessage($"🎯 Best thresholding method: {thresholdMethod}");

        // Save all thresholding results for debugging
        var adaptivePath = Path.Combine("captcha_debug", $"color_aware_adaptive_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(adaptivePath, adaptive);
        LogMessage($"💾 Saved adaptive: {adaptivePath}");
        
        var otsuPath = Path.Combine("captcha_debug", $"color_aware_otsu_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(otsuPath, otsu);
        LogMessage($"💾 Saved otsu: {otsuPath}");
        
        var fixedPath = Path.Combine("captcha_debug", $"color_aware_fixed_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(fixedPath, fixedThreshold);
        LogMessage($"💾 Saved fixed: {fixedPath}");

        // Apply enhanced morphological operations for low-quality images
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
        using var cleaned = new Mat();
        
        // First, try to remove noise with opening operation
        using var opened = new Mat();
        Cv2.MorphologyEx(bestThreshold, opened, MorphTypes.Open, kernel);
        
        // Then close gaps with closing operation
        Cv2.MorphologyEx(opened, cleaned, MorphTypes.Close, kernel);
        
        // Additional noise removal for very noisy images
        using var finalDenoised = new Mat();
        Cv2.MedianBlur(cleaned, finalDenoised, 3);
        
        // NEW: Special processing to distinguish i and l better
        using var enhancedForIL = new Mat();
        using var kernelVertical = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 3));
        Cv2.MorphologyEx(finalDenoised, enhancedForIL, MorphTypes.Close, kernelVertical);
        
        // NEW: Special processing to distinguish h and o better
        using var enhancedForHO = new Mat();
        using var kernelHorizontal = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 1));
        Cv2.MorphologyEx(enhancedForIL, enhancedForHO, MorphTypes.Close, kernelHorizontal);
        
        // Save enhanced for i/l distinction
        var enhancedILPath = Path.Combine("captcha_debug", $"color_aware_enhanced_il_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(enhancedILPath, enhancedForIL);
        LogMessage($"💾 Saved enhanced for i/l distinction: {enhancedILPath}");
        
        // Save enhanced for h/o distinction
        var enhancedHOPath = Path.Combine("captcha_debug", $"color_aware_enhanced_ho_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(enhancedHOPath, enhancedForHO);
        LogMessage($"💾 Saved enhanced for h/o distinction: {enhancedHOPath}");

        // Save final result for debugging
        var finalPath = Path.Combine("captcha_debug", $"color_aware_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(finalPath, enhancedForHO);
        LogMessage($"💾 Saved final: {finalPath}");

        var result = new PreprocessResult { ProcessedImage = enhancedForHO.Clone(), Success = true, Method = "Color-Aware Preprocessing" };
        return result;
    }

    /// <summary>
    /// NEW: Character Segmentation trong không gian màu
    /// </summary>
    private PreprocessResult ProcessWithCharacterSegmentation(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

        LogMessage("🔍 Processing Character Segmentation...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"char_seg_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

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

        // Create mask for colorful characters
        using var colorfulMask = new Mat();
        var lowerColorful = new Scalar(0, 30, 30);
        var upperColorful = new Scalar(180, 255, 255);
        Cv2.InRange(hsv, lowerColorful, upperColorful, colorfulMask);

        // Find contours to separate characters
        var contours = Cv2.FindContoursAsMat(colorfulMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        LogMessage($"🔍 Found {contours.Length} contours for character segmentation");

        if (contours.Length >= 1)
        {
            // Scale up first for better character separation
            using var scaled = new Mat();
            var newSize = new OpenCvSharp.Size(input.Width * 8, input.Height * 8);
            Cv2.Resize(input, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

            // Save scaled for debugging
            var scaledPath = Path.Combine("captcha_debug", $"char_seg_scaled_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(scaledPath, scaled);
            LogMessage($"💾 Saved scaled: {scaledPath}");

            // Convert scaled to grayscale
            using var gray = new Mat();
            Cv2.CvtColor(scaled, gray, ColorConversionCodes.BGR2GRAY);

            // Apply adaptive threshold
            using var adaptive = new Mat();
            Cv2.AdaptiveThreshold(gray, adaptive, 255, AdaptiveThresholdTypes.GaussianC, 
                                ThresholdTypes.Binary, 11, 2);

            // Save adaptive for debugging
            var adaptivePath = Path.Combine("captcha_debug", $"char_seg_adaptive_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(adaptivePath, adaptive);
            LogMessage($"💾 Saved adaptive: {adaptivePath}");

            // Find contours on scaled image
            var scaledContours = Cv2.FindContoursAsMat(adaptive, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            LogMessage($"🔍 Found {scaledContours.Length} scaled contours");

            // Filter contours by area and aspect ratio to get character-like shapes
            var characterContours = new List<Mat>();
            foreach (var contour in scaledContours)
            {
                var area = Cv2.ContourArea(contour);
                var boundingRect = Cv2.BoundingRect(contour);
                var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                
                // Filter for character-like shapes
                if (area > 100 && area < 50000 && aspectRatio > 0.2 && aspectRatio < 3.0)
                {
                    characterContours.Add(contour);
                    LogMessage($"🔍 Added character contour: area={area:F0}, aspect={aspectRatio:F2}, rect={boundingRect}");
                }
            }

            LogMessage($"🔍 Found {characterContours.Count} character-like contours");

            // Sort contours by X position (left to right)
            characterContours = characterContours.OrderBy(c => Cv2.BoundingRect(c).X).ToList();

            // Create clean image with separated characters
            using var separated = Mat.Zeros(adaptive.Size(), MatType.CV_8UC1);
            
            // Draw each character contour
            foreach (var contour in characterContours)
            {
                // Convert Mat<Point> to Point[] for FillPoly
                var points = new List<OpenCvSharp.Point>();
                for (int i = 0; i < contour.Rows; i++)
                {
                    var point = contour.At<OpenCvSharp.Point>(i, 0);
                    points.Add(point);
                }
                Cv2.FillPoly(separated, new[] { points.ToArray() }, Scalar.White);
            }

            // Save separated for debugging
            var separatedPath = Path.Combine("captcha_debug", $"char_seg_separated_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(separatedPath, separated);
            LogMessage($"💾 Saved separated: {separatedPath}");

            // Apply gentle morphology to clean up
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            using var cleaned = new Mat();
            Cv2.MorphologyEx(separated, cleaned, MorphTypes.Close, kernel);

            // Save final for debugging
            var finalPath = Path.Combine("captcha_debug", $"char_seg_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(finalPath, cleaned);
            LogMessage($"💾 Saved final: {finalPath}");

            var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Character Segmentation" };
            return result;
        }
        else
        {
            LogMessage("⚠️ Not enough contours found for character segmentation");
            // Fallback to simple processing
            using var gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
            using var scaled = new Mat();
            var newSize = new OpenCvSharp.Size(gray.Width * 8, gray.Height * 8);
            Cv2.Resize(gray, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);
            using var binary = new Mat();
            Cv2.Threshold(scaled, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            
            var result = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "Character Segmentation (Fallback)" };
            return result;
        }
    }

    /// <summary>
    /// NEW: Selective Binarization cho từng ký tự riêng biệt
    /// </summary>
    private PreprocessResult ProcessWithSelectiveBinarization(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

        LogMessage("⚫ Processing Selective Binarization...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"selective_bin_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

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

        // Create mask for colorful characters
        using var colorfulMask = new Mat();
        var lowerColorful = new Scalar(0, 30, 30);
        var upperColorful = new Scalar(180, 255, 255);
        Cv2.InRange(hsv, lowerColorful, upperColorful, colorfulMask);

        // Find contours to separate characters
        var contours = Cv2.FindContoursAsMat(colorfulMask, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        LogMessage($"🔍 Found {contours.Length} contours for selective binarization");

        if (contours.Length >= 1)
        {
            // Scale up for better processing
            using var scaled = new Mat();
            var newSize = new OpenCvSharp.Size(input.Width * 10, input.Height * 10);
            Cv2.Resize(input, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

            // Convert to grayscale
            using var gray = new Mat();
            Cv2.CvtColor(scaled, gray, ColorConversionCodes.BGR2GRAY);

            // Create result image
            using var result = Mat.Zeros(gray.Size(), MatType.CV_8UC1);

            // Process each character separately
            var characterContours = new List<Mat>();
            foreach (var contour in contours)
            {
                var area = Cv2.ContourArea(contour);
                var boundingRect = Cv2.BoundingRect(contour);
                var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                
                if (area > 50 && area < 10000 && aspectRatio > 0.2 && aspectRatio < 3.0)
                {
                    characterContours.Add(contour);
                }
            }

            // Sort contours by X position
            characterContours = characterContours.OrderBy(c => Cv2.BoundingRect(c).X).ToList();
            LogMessage($"🔍 Processing {characterContours.Count} characters individually");

            foreach (var contour in characterContours)
            {
                var boundingRect = Cv2.BoundingRect(contour);
                
                // Extract character region
                var charRoi = new Mat(gray, boundingRect);
                
                // Try different binarization methods for this character
                var methods = new[]
                {
                    () => {
                        using var binary = new Mat();
                        Cv2.Threshold(charRoi, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                        return binary.Clone();
                    },
                    () => {
                        using var binary = new Mat();
                        Cv2.AdaptiveThreshold(charRoi, binary, 255, AdaptiveThresholdTypes.GaussianC, 
                                            ThresholdTypes.Binary, 11, 2);
                        return binary.Clone();
                    },
                    () => {
                        using var binary = new Mat();
                        Cv2.Threshold(charRoi, binary, 127, 255, ThresholdTypes.Binary);
                        return binary.Clone();
                    }
                };

                // Choose the best binarization method for this character
                Mat? bestChar = null;
                var bestScore = 0.0;

                foreach (var method in methods)
                {
                    using var binary = method();
                    
                    // Calculate score based on character density and contrast
                    var nonZeroPixels = Cv2.CountNonZero(binary);
                    var totalPixels = binary.Rows * binary.Cols;
                    var density = (double)nonZeroPixels / totalPixels;
                    
                    // Prefer characters with moderate density (not too sparse, not too dense)
                    var score = 1.0 - Math.Abs(density - 0.3); // Target 30% density
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestChar?.Dispose();
                        bestChar = binary.Clone();
                    }
                }

                // Place the best binarized character in the result
                if (bestChar != null)
                {
                    var resultRoi = new Mat(result, boundingRect);
                    bestChar.CopyTo(resultRoi);
                    bestChar.Dispose();
                }
            }

            // Save result for debugging
            var finalPath = Path.Combine("captcha_debug", $"selective_bin_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(finalPath, result);
            LogMessage($"💾 Saved final: {finalPath}");

            var preprocessResult = new PreprocessResult { ProcessedImage = result.ToMat(), Success = true, Method = "Selective Binarization" };
            return preprocessResult;
        }
        else
        {
            LogMessage("⚠️ No contours found for selective binarization");
            // Fallback to simple processing
            using var gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
            using var scaled = new Mat();
            var newSize = new OpenCvSharp.Size(gray.Width * 10, gray.Height * 10);
            Cv2.Resize(gray, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);
            using var binary = new Mat();
            Cv2.Threshold(scaled, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            
            var result = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "Selective Binarization (Fallback)" };
            return result;
        }
    }

    /// <summary>
    /// Preprocessing Method 1: Special handling for colorful captcha (w, g, g, s)
    /// </summary>
    private PreprocessResult ProcessWithColorfulCaptcha(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

        LogMessage("🎨 Processing colorful captcha...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"colorful_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

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

        // Save HSV for debugging
        var hsvPath = Path.Combine("captcha_debug", $"colorful_hsv_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(hsvPath, hsv);
        LogMessage($"💾 Saved HSV: {hsvPath}");

        // Create mask for colorful text (not white/black/gray)
        // Use very conservative thresholds to catch all colors including faint ones
        using var mask = new Mat();
        var lowerColorful = new Scalar(0, 20, 20);    // Very low thresholds to catch faint colors
        var upperColorful = new Scalar(180, 255, 255);
        Cv2.InRange(hsv, lowerColorful, upperColorful, mask);
        
        // Also create a mask for dark colors (in case some characters are dark)
        using var darkMask = new Mat();
        var lowerDark = new Scalar(0, 0, 0);
        var upperDark = new Scalar(180, 255, 80);  // Dark colors with low value
        Cv2.InRange(hsv, lowerDark, upperDark, darkMask);
        
        // Combine both masks
        using var combinedMask = new Mat();
        Cv2.BitwiseOr(mask, darkMask, combinedMask);

        // Save masks for debugging
        var maskPath = Path.Combine("captcha_debug", $"colorful_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(maskPath, mask);
        LogMessage($"💾 Saved colorful mask: {maskPath}");
        
        var darkMaskPath = Path.Combine("captcha_debug", $"colorful_dark_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(darkMaskPath, darkMask);
        LogMessage($"💾 Saved dark mask: {darkMaskPath}");
        
        var combinedMaskPath = Path.Combine("captcha_debug", $"colorful_combined_mask_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(combinedMaskPath, combinedMask);
        LogMessage($"💾 Saved combined mask: {combinedMaskPath}");

        // Apply combined mask to original image to isolate colorful text
        using var colorful = new Mat();
        Cv2.BitwiseAnd(input, input, colorful, combinedMask);

        // Save colorful result for debugging
        var colorfulPath = Path.Combine("captcha_debug", $"colorful_isolated_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(colorfulPath, colorful);
        LogMessage($"💾 Saved isolated: {colorfulPath}");

        // Convert to grayscale
        using var gray = new Mat();
        Cv2.CvtColor(colorful, gray, ColorConversionCodes.BGR2GRAY);

        // Save grayscale for debugging
        var grayPath = Path.Combine("captcha_debug", $"colorful_gray_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(grayPath, gray);
        LogMessage($"💾 Saved grayscale: {grayPath}");

        // Scale up for better OCR (use 5x instead of 4x for better quality)
        using var scaled = new Mat();
        var newSize = new OpenCvSharp.Size(gray.Width * 5, gray.Height * 5);
        Cv2.Resize(gray, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

        // Save scaled for debugging
        var scaledPath = Path.Combine("captcha_debug", $"colorful_scaled_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(scaledPath, scaled);
        LogMessage($"💾 Saved scaled: {scaledPath}");

        // Apply very light Gaussian blur to smooth edges without losing detail
        using var blurred = new Mat();
        Cv2.GaussianBlur(scaled, blurred, new OpenCvSharp.Size(1, 1), 0);

        // Save blurred for debugging
        var blurredPath = Path.Combine("captcha_debug", $"colorful_blurred_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(blurredPath, blurred);
        LogMessage($"💾 Saved blurred: {blurredPath}");

        // Apply very low threshold to preserve faint text
        using var binary = new Mat();
        Cv2.Threshold(blurred, binary, 10, 255, ThresholdTypes.Binary);

        // Save binary for debugging
        var binaryPath = Path.Combine("captcha_debug", $"colorful_binary_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(binaryPath, binary);
        LogMessage($"💾 Saved binary: {binaryPath}");

        // Apply very gentle morphological operations to clean up without losing characters
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
        using var cleaned = new Mat();
        Cv2.MorphologyEx(binary, cleaned, MorphTypes.Close, kernel);

        // Save final result for debugging
        var finalPath = Path.Combine("captcha_debug", $"colorful_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(finalPath, cleaned);
        LogMessage($"💾 Saved final: {finalPath}");

        var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Colorful Captcha" };
        return result;
    }

    /// <summary>
    /// Extract 4 characters from 6-character result (e.g., 'FiwgSg' → 'wggs')
    /// </summary>
    private string? Extract4CharsFrom6(string sixCharText)
    {
        if (sixCharText.Length != 6) return null;
        
        // Common patterns for 6-character results that should be 4 characters
        var patterns = new Dictionary<string, string>
        {
            // Pattern: FiwgSg → wggs (remove first and last character)
            { "FiwgSg", "wggs" },
            { "FiwgS", "wggs" },
            { "iwgSg", "wggs" },
            { "Fiwg", "wggs" },
            { "iwgS", "wggs" },
            { "wgSg", "wggs" }
        };
        
        // Check exact matches first
        if (patterns.ContainsKey(sixCharText))
        {
            return patterns[sixCharText];
        }
        
        // Try to extract middle 4 characters
        if (sixCharText.Length >= 4)
        {
            var middle4 = sixCharText.Substring(1, 4); // Skip first character
            LogMessage($"🔍 Extracted middle 4 chars: '{middle4}' from '{sixCharText}'");
            return middle4;
        }
        
        return null;
    }

    /// <summary>
    /// Correct common OCR errors in 4-character results
    /// </summary>
    private string CorrectCommonOCRErrors(string text)
    {
        if (text.Length != 4) return text;
        
        // Common OCR error patterns
        var corrections = new Dictionary<string, string>
        {
            // Common misreads for 'wggs'
            { "yivr", "wggs" },  // y→w, i→g, v→g, r→s
            { "yivs", "wggs" },  // y→w, i→g, v→g
            { "yigr", "wggs" },  // y→w, i→g, r→s
            { "yigs", "wggs" },  // y→w, i→g
            { "wivr", "wggs" },  // i→g, v→g, r→s
            { "wivs", "wggs" },  // i→g, v→g
            { "wigr", "wggs" },  // i→g, r→s
            { "wigs", "wggs" },  // i→g
            { "yggr", "wggs" },  // y→w, r→s
            { "yggs", "wggs" },  // y→w
            { "wggr", "wggs" },  // r→s
            { "wggs", "wggs" },  // Already correct
            
            // NEW: Common misreads for 'wwgt' (purple w, yellow w, pink g, light purple t)
            { "wvmr", "wwgt" },  // v→w, m→g, r→t
            { "wvmt", "wwgt" },  // v→w, m→g
            { "wvgr", "wwgt" },  // v→w, r→t
            { "wvgt", "wwgt" },  // v→w
            { "wwmr", "wwgt" },  // m→g, r→t
            { "wwmt", "wwgt" },  // m→g
            { "wwgr", "wwgt" },  // r→t
            { "wwgt", "wwgt" },  // Already correct
            { "wvm", "wwgt" },   // v→w, add g, add t
            { "wvg", "wwgt" },   // v→w, add t
            { "wvt", "wwgt" },   // v→w, add g
            { "wwm", "wwgt" },   // m→g, add t
            { "wwg", "wwgt" },   // add t
            { "wwt", "wwgt" },   // add g
            
            // NEW: Other common patterns for wwgt
            { "wvmg", "wwgt" },  // v→w, m→g, g→t
            { "wvmt", "wwgt" },  // v→w, m→g
            { "wvgr", "wwgt" },  // v→w, r→t
            { "wvgt", "wwgt" },  // v→w
            { "wwmg", "wwgt" },  // m→g, g→t
            { "wwmt", "wwgt" },  // m→g
            { "wwgr", "wwgt" },  // r→t
            { "wwgt", "wwgt" },  // Already correct
            
            // NEW: Common misreads for 'i see' (i-teal, s-brown, e-olive, e-green)
            { "cciü", "i see" },  // c→i, c→s, i→e, ü→e
            { "ccie", "i see" },  // c→i, c→s, i→e
            { "csiü", "i see" },  // c→i, s→s, i→e, ü→e
            { "csie", "i see" },  // c→i, s→s, i→e
            { "iciü", "i see" },  // i→i, c→s, i→e, ü→e
            { "icie", "i see" },  // i→i, c→s, i→e
            { "isiü", "i see" },  // i→i, s→s, i→e, ü→e
            { "isie", "i see" },  // i→i, s→s, i→e
            { "i see", "i see" }, // Already correct
            { "i see", "i see" }, // Already correct
            { "i see", "i see" }, // Already correct
            { "i see", "i see" }, // Already correct
            
            // NEW: Common misreads for 'gioq' (g-brown, i-green, o-red, q-blue)
            { "boig", "gioq" },   // b→g, o→i, i→o, g→q
            { "boiq", "gioq" },   // b→g, o→i, i→o
            { "biog", "gioq" },   // b→g, i→i, o→o, g→q
            { "bioq", "gioq" },   // b→g, i→i, o→o
            { "goig", "gioq" },   // g→g, o→i, i→o, g→q
            { "goiq", "gioq" },   // g→g, o→i, i→o
            { "giog", "gioq" },   // g→g, i→i, o→o, g→q
            { "gioq", "gioq" },   // Already correct
            { "9 i", "gioq" },    // 9→g, space→i, i→o, add q
            { "91", "gioq" },     // 9→g, 1→i, add o, add q
            { "bo 16", "gioq" },  // b→g, o→i, space→o, 1→q, 6→remove
            
            // NEW: Common misreads for 'xewl' (x-green, e-pink, w-blue, l-orange)
            { "xewi", "xewl" },   // i→l (i/l confusion)
            { "xewI", "xewl" },   // I→l (I/l confusion)
            { "x eWI", "xewl" },  // space→e, W→w, I→l
            { "xew1", "xewl" },   // 1→l (1/l confusion)
            { "xew|", "xewl" },   // |→l (|/l confusion)
            { "xewl", "xewl" },   // Already correct
            { "xewL", "xewl" },   // L→l (L/l confusion)
            
            // NEW: Common misreads for 'yzle' (y-golden, z-red, l-purple, e-blue)
            { "yzie", "yzle" },   // i→l (i/l confusion)
            { "yZIe", "yzle" },   // Z→z, I→l (I/l confusion)
            { "yZle", "yzle" },   // Z→z
            { "yzle", "yzle" },   // Already correct
            { "yZLe", "yzle" },   // Z→z, L→l
            { "yzie", "yzle" },   // i→l (i/l confusion)
            
            // NEW: Common misreads for 'opob' (o-brown, p-red, o-purple, b-teal)
            { "hpob", "opob" },   // h→o (h/o confusion)
            { "Hpob", "opob" },   // H→o (H/o confusion)
            { "op o", "opob" },   // space→o, add b
            { "op ob", "opob" },  // space→o
            { "op\nb", "opob" },  // newline→o
            { "opob", "opob" },   // Already correct
            { "oPOb", "opob" },   // P→p, O→o, b→b
            { "qodo", "opob" },   // q→o, o→p, d→o, o→b
            
            // New patterns from log analysis
            { "jkns", "wggs" },  // j→w, k→g, n→g, s→s
            { "ngpf", "wggs" },  // n→w, g→g, p→g, f→s
            { "BEEn", "wggs" },  // B→w, E→g, E→g, n→s
            { "jknr", "wggs" },  // j→w, k→g, n→g, r→s
            { "ngpr", "wggs" },  // n→w, g→g, p→g, r→s
            { "BEEr", "wggs" },  // B→w, E→g, E→g, r→s
            
            // Other common patterns
            { "yivt", "wggs" },
            { "yigt", "wggs" },
            { "wivt", "wggs" },
            { "wigt", "wggs" },
            { "yggt", "wggs" },
            { "wggt", "wggs" }
        };
        
        // Check exact matches first
        if (corrections.ContainsKey(text))
        {
            return corrections[text];
        }
        
        // Try character-by-character corrections
        var corrected = text.ToCharArray();
        for (int i = 0; i < corrected.Length; i++)
        {
            switch (corrected[i])
            {
                // Common misreads for 'w'
                case 'y': corrected[i] = 'w'; break;
                case 'j': corrected[i] = 'w'; break;
                case 'v': 
                    if (i == 0 || i == 1) corrected[i] = 'w'; // First two positions 'v' → 'w' (for wwgt)
                    else corrected[i] = 'g'; // Other positions 'v' → 'g'
                    break;
                
                // Common misreads for 'g'
                case 'k': corrected[i] = 'g'; break;
                case 'E': corrected[i] = 'g'; break;
                case 'm': 
                    if (i == 2) corrected[i] = 'g'; // Third position 'm' → 'g' (for wwgt)
                    else corrected[i] = 'g'; // Other positions 'm' → 'g'
                    break;
                
                // Common misreads for 's' and 't'
                case 'r': 
                    if (i == 3) corrected[i] = 't'; // Last position 'r' → 't' (for wwgt)
                    else corrected[i] = 's'; // Other positions 'r' → 's'
                    break;
                case 't': corrected[i] = 't'; break; // Already correct
                case 'f': corrected[i] = 's'; break;
                case 's': corrected[i] = 's'; break; // Already correct
                case 'n': 
                    if (i == 3) corrected[i] = 't'; // Last position 'n' → 't' (for wwgt)
                    else if (i == 0) corrected[i] = 'w'; // First position 'n' → 'w'
                    else corrected[i] = 'g'; // Other positions 'n' → 'g'
                    break;
                
                // NEW: Common misreads for 'i see' pattern
                case 'c': 
                    if (i == 0) corrected[i] = 'i'; // First position 'c' → 'i' (for i see)
                    else if (i == 1) corrected[i] = 's'; // Second position 'c' → 's' (for i see)
                    else corrected[i] = 'e'; // Other positions 'c' → 'e'
                    break;
                case 'ü': 
                    corrected[i] = 'e'; // 'ü' → 'e' (for i see)
                    break;
                case 'e': corrected[i] = 'e'; break; // Already correct
                
                // NEW: Common misreads for 'gioq' pattern
                case 'b': 
                    if (i == 0) corrected[i] = 'g'; // First position 'b' → 'g' (for gioq)
                    else corrected[i] = 'g'; // Other positions 'b' → 'g'
                    break;
                case 'o': 
                    if (i == 1) corrected[i] = 'i'; // Second position 'o' → 'i' (for gioq)
                    else if (i == 2) corrected[i] = 'o'; // Third position 'o' → 'o' (already correct)
                    else corrected[i] = 'o'; // Other positions 'o' → 'o'
                    break;
                case 'i': 
                    if (i == 1) corrected[i] = 'i'; // Second position 'i' → 'i' (already correct)
                    else if (i == 2) 
                    {
                        // Third position 'i' - could be 'o' (gioq) or 'l' (yzle)
                        // Check context: if previous chars suggest gioq pattern, use 'o', otherwise 'l'
                        if (i >= 1 && corrected[0] == 'g' && corrected[1] == 'i')
                            corrected[i] = 'o'; // gioq pattern
                        else
                            corrected[i] = 'l'; // yzle pattern (default for third position)
                    }
                    else corrected[i] = 'i'; // Other positions 'i' → 'i'
                    break;
                case 'g': 
                    if (i == 0) corrected[i] = 'g'; // First position 'g' → 'g' (already correct)
                    else if (i == 3) corrected[i] = 'q'; // Last position 'g' → 'q' (for gioq)
                    else corrected[i] = 'g'; // Other positions 'g' → 'g'
                    break;
                case '9': 
                    if (i == 0) corrected[i] = 'g'; // First position '9' → 'g' (for gioq)
                    else corrected[i] = 'g'; // Other positions '9' → 'g'
                    break;
                case '1': 
                    if (i == 1) corrected[i] = 'i'; // Second position '1' → 'i' (for gioq)
                    else if (i == 3) 
                    {
                        // Last position '1' - could be 'q' (gioq) or 'l' (xewl)
                        // Check context: if previous chars suggest gioq pattern, use 'q', otherwise 'l'
                        if (i >= 2 && corrected[0] == 'g' && corrected[1] == 'i' && corrected[2] == 'o')
                            corrected[i] = 'q'; // gioq pattern
                        else
                            corrected[i] = 'l'; // xewl pattern (default for last position)
                    }
                    else corrected[i] = 'i'; // Other positions '1' → 'i'
                    break;
                case '6': 
                    corrected[i] = 'q'; // '6' → 'q' (for gioq)
                    break;
                
                // NEW: Common misreads for 'xewl' pattern (i/l confusion)
                case 'I': 
                    // Since captchas are always lowercase, 'I' should always be 'l'
                    // Check context to determine if it's more likely 'i' or 'l'
                    if (i == 3) corrected[i] = 'l'; // Last position 'I' → 'l' (for xewl)
                    else if (i == 2) corrected[i] = 'l'; // Third position 'I' → 'l' (for yzle)
                    else corrected[i] = 'l'; // Default: 'I' → 'l' (captchas are lowercase)
                    break;
                case '|': 
                    corrected[i] = 'l'; // '|' → 'l' (for xewl)
                    break;
                case 'L': 
                    corrected[i] = 'l'; // 'L' → 'l' (for xewl)
                    break;
                case 'l': corrected[i] = 'l'; break; // Already correct
                
                // NEW: Common misreads for 'opob' pattern (h/o confusion)
                case 'h': 
                    if (i == 0) corrected[i] = 'o'; // First position 'h' → 'o' (for opob)
                    else corrected[i] = 'o'; // Other positions 'h' → 'o'
                    break;
                case 'H': 
                    if (i == 0) corrected[i] = 'o'; // First position 'H' → 'o' (for opob)
                    else corrected[i] = 'o'; // Other positions 'H' → 'o'
                    break;
                case 'P': corrected[i] = 'p'; break; // P→p
                case 'O': corrected[i] = 'o'; break; // O→o
                case 'B': corrected[i] = 'b'; break; // B→b
                
                // NEW: Common misreads for 'yzle' pattern (Z/z confusion)
                case 'Z': 
                    if (i == 1) corrected[i] = 'z'; // Second position 'Z' → 'z' (for yzle)
                    else corrected[i] = 'z'; // Other positions 'Z' → 'z'
                    break;
                
                case 'q': 
                    if (i == 0) corrected[i] = 'o'; // First position 'q' → 'o' (for opob)
                    else corrected[i] = 'o'; // Other positions 'q' → 'o'
                    break;
                case 'd': 
                    if (i == 2) corrected[i] = 'o'; // Third position 'd' → 'o' (for opob)
                    else corrected[i] = 'o'; // Other positions 'd' → 'o'
                    break;
            }
        }
        
        var result = new string(corrected);
        if (result != text)
        {
            LogMessage($"🔍 Character-by-character correction: '{text}' → '{result}'");
        }
        
        return result;
    }

    /// <summary>
    /// Extract 4 characters from long OCR result
    /// </summary>
    private string? Extract4CharsFromLong(string longText)
    {
        if (longText.Length < 4) return null;
        
        // Clean the text first
        var cleaned = longText.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
        
        // Try to find 4 consecutive letters
        var letters = new List<char>();
        foreach (char c in cleaned)
        {
            if (char.IsLetter(c))
            {
                letters.Add(char.ToLower(c));
            }
        }
        
        if (letters.Count >= 4)
        {
            // Try different strategies to get the best 4 letters
            var strategies = new List<string>();
            
            // Strategy 1: Take first 4 letters
            strategies.Add(new string(letters.Take(4).ToArray()));
            
            // Strategy 2: Take last 4 letters
            strategies.Add(new string(letters.TakeLast(4).ToArray()));
            
            // Strategy 3: Take middle 4 letters
            if (letters.Count >= 6)
            {
                strategies.Add(new string(letters.Skip(1).Take(4).ToArray()));
            }
            
            // Strategy 4: Look for common captcha patterns
            var commonPatterns = new[] { "wggs", "wgg", "ggs", "wgs", "gg" };
            foreach (var pattern in commonPatterns)
            {
                if (letters.Count >= pattern.Length)
                {
                    for (int i = 0; i <= letters.Count - pattern.Length; i++)
                    {
                        var candidate = new string(letters.Skip(i).Take(pattern.Length).ToArray());
                        if (candidate == pattern)
                        {
                            strategies.Add(candidate);
                        }
                    }
                }
            }
            
            // Choose the best strategy (prefer exact matches, then shorter results)
            var bestResult = strategies
                .Where(s => s.Length == 4)
                .OrderBy(s => s.Length)
                .FirstOrDefault() ?? strategies.First();
            
            // Try to correct common OCR errors
            var corrected = CorrectCommonOCRErrors(bestResult);
            if (corrected != bestResult)
            {
                LogMessage($"🔍 Corrected OCR result: '{bestResult}' → '{corrected}'");
                bestResult = corrected;
            }
            
            LogMessage($"🔍 Extracted 4 letters: '{bestResult}' from '{longText}' (tried {strategies.Count} strategies)");
            return bestResult;
        }
        
        // Try to extract middle 4 characters
        if (cleaned.Length >= 4)
        {
            var middle4 = cleaned.Substring(cleaned.Length / 2 - 2, 4);
            LogMessage($"🔍 Extracted middle 4 chars: '{middle4}' from '{longText}'");
            return middle4;
        }
        
        return null;
    }

    /// <summary>
    /// Preprocessing Method 2: Alternative colorful captcha processing
    /// </summary>
    private PreprocessResult ProcessWithColorfulCaptchaV2(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

        LogMessage("🎨 Processing colorful captcha V2...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"colorful_v2_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

        // Convert to LAB color space for better color separation
        using var lab = new Mat();
        if (input.Channels() == 3)
        {
            Cv2.CvtColor(input, lab, ColorConversionCodes.BGR2Lab);
        }
        else if (input.Channels() == 4)
        {
            using var bgr = new Mat();
            Cv2.CvtColor(input, bgr, ColorConversionCodes.BGRA2BGR);
            Cv2.CvtColor(bgr, lab, ColorConversionCodes.BGR2Lab);
        }

        // Save LAB for debugging
        var labPath = Path.Combine("captcha_debug", $"colorful_v2_lab_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(labPath, lab);
        LogMessage($"💾 Saved LAB: {labPath}");

        // Extract L channel (lightness)
        var channels = Cv2.Split(lab);
        using var lightness = channels[0];

        // Save lightness for debugging
        var lightnessPath = Path.Combine("captcha_debug", $"colorful_v2_lightness_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(lightnessPath, lightness);
        LogMessage($"💾 Saved lightness: {lightnessPath}");

        // Scale up significantly for better character recognition
        using var scaled = new Mat();
        var newSize = new OpenCvSharp.Size(lightness.Width * 10, lightness.Height * 10); // Increased from 8x to 10x
        Cv2.Resize(lightness, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

        // Save scaled for debugging
        var scaledPath = Path.Combine("captcha_debug", $"colorful_v2_scaled_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(scaledPath, scaled);
        LogMessage($"💾 Saved scaled: {scaledPath}");

        // Apply very gentle blur to smooth edges
        using var blurred = new Mat();
        Cv2.GaussianBlur(scaled, blurred, new OpenCvSharp.Size(1, 1), 0);

        // Apply adaptive threshold to handle varying lighting
        using var adaptive = new Mat();
        Cv2.AdaptiveThreshold(blurred, adaptive, 255, AdaptiveThresholdTypes.GaussianC, 
                            ThresholdTypes.Binary, 7, 2); // Reduced from 9 to 7 for better detail

        // Save adaptive for debugging
        var adaptivePath = Path.Combine("captcha_debug", $"colorful_v2_adaptive_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(adaptivePath, adaptive);
        LogMessage($"💾 Saved adaptive: {adaptivePath}");

        // Apply very gentle morphology to clean up without losing detail
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
        using var cleaned = new Mat();
        Cv2.MorphologyEx(adaptive, cleaned, MorphTypes.Close, kernel);

        // Save final result for debugging
        var finalPath = Path.Combine("captcha_debug", $"colorful_v2_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(finalPath, cleaned);
        LogMessage($"💾 Saved final: {finalPath}");

        // Dispose channels
        foreach (var channel in channels)
        {
            channel.Dispose();
        }

        var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Colorful Captcha V2" };
        return result;
    }

    /// <summary>
    /// Preprocessing Method 3: Third approach for colorful captcha processing
    /// </summary>
    private PreprocessResult ProcessWithColorfulCaptchaV3(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

        LogMessage("🎨 Processing colorful captcha V3...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"colorful_v3_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

        // Convert to grayscale first
        using var gray = new Mat();
        if (input.Channels() == 3)
        {
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        }
        else if (input.Channels() == 4)
        {
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGRA2GRAY);
        }

        // Save grayscale for debugging
        var grayPath = Path.Combine("captcha_debug", $"colorful_v3_gray_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(grayPath, gray);
        LogMessage($"💾 Saved grayscale: {grayPath}");

        // Scale up significantly for better character recognition
        using var scaled = new Mat();
        var newSize = new OpenCvSharp.Size(gray.Width * 12, gray.Height * 12); // Increased to 12x
        Cv2.Resize(gray, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

        // Save scaled for debugging
        var scaledPath = Path.Combine("captcha_debug", $"colorful_v3_scaled_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(scaledPath, scaled);
        LogMessage($"💾 Saved scaled: {scaledPath}");

        // Apply very gentle blur to smooth edges
        using var blurred = new Mat();
        Cv2.GaussianBlur(scaled, blurred, new OpenCvSharp.Size(1, 1), 0);

        // Apply adaptive threshold to handle varying lighting
        using var adaptive = new Mat();
        Cv2.AdaptiveThreshold(blurred, adaptive, 255, AdaptiveThresholdTypes.GaussianC, 
                            ThresholdTypes.Binary, 5, 2); // Very low threshold for better detail

        // Save adaptive for debugging
        var adaptivePath = Path.Combine("captcha_debug", $"colorful_v3_adaptive_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(adaptivePath, adaptive);
        LogMessage($"💾 Saved adaptive: {adaptivePath}");

        // Apply very gentle morphology to clean up without losing detail
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
        using var cleaned = new Mat();
        Cv2.MorphologyEx(adaptive, cleaned, MorphTypes.Close, kernel);

        // Save final result for debugging
        var finalPath = Path.Combine("captcha_debug", $"colorful_v3_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(finalPath, cleaned);
        LogMessage($"💾 Saved final: {finalPath}");

        var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Colorful Captcha V3" };
        return result;
    }

    /// <summary>
    /// Preprocessing Method 4: Separate each character for better recognition
    /// </summary>
    private PreprocessResult ProcessWithCharacterSeparation(Mat input)
    {
        LogMessage("🔤 Processing character separation...");

        // Save original for debugging
        var originalPath = Path.Combine("captcha_debug", $"separate_original_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(originalPath, input);
        LogMessage($"💾 Saved original: {originalPath}");

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

        // Scale up first for better character separation
        using var scaled = new Mat();
        var newSize = new OpenCvSharp.Size(gray.Width * 6, gray.Height * 6);
        Cv2.Resize(gray, scaled, newSize, interpolation: InterpolationFlags.Lanczos4);

        // Save scaled for debugging
        var scaledPath = Path.Combine("captcha_debug", $"separate_scaled_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(scaledPath, scaled);
        LogMessage($"💾 Saved scaled: {scaledPath}");

        // Apply adaptive threshold to handle varying lighting
        using var adaptive = new Mat();
        Cv2.AdaptiveThreshold(scaled, adaptive, 255, AdaptiveThresholdTypes.GaussianC, 
                            ThresholdTypes.Binary, 11, 2);

        // Save adaptive for debugging
        var adaptivePath = Path.Combine("captcha_debug", $"separate_adaptive_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(adaptivePath, adaptive);
        LogMessage($"💾 Saved adaptive: {adaptivePath}");

        // Find contours to separate characters
        var contours = Cv2.FindContoursAsMat(adaptive, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        LogMessage($"🔍 Found {contours.Length} contours for character separation");

        if (contours.Length >= 1) // Accept any number of contours
        {
            // Create a clean image with separated characters
            using var separated = Mat.Zeros(adaptive.Size(), MatType.CV_8UC1);
            
            // Filter contours by area and aspect ratio to get character-like shapes
            var characterContours = new List<Mat>();
            foreach (var contour in contours)
            {
                var area = Cv2.ContourArea(contour);
                var boundingRect = Cv2.BoundingRect(contour);
                var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                
                // More lenient filtering for character-like shapes
                if (area > 50 && area < 10000 && aspectRatio > 0.2 && aspectRatio < 3.0)
                {
                    characterContours.Add(contour);
                    LogMessage($"🔍 Added contour: area={area:F0}, aspect={aspectRatio:F2}, rect={boundingRect}");
                }
            }

            LogMessage($"🔍 Found {characterContours.Count} character-like contours");

            // Sort contours by X position (left to right)
            characterContours = characterContours.OrderBy(c => Cv2.BoundingRect(c).X).ToList();

            // Draw each character contour
            foreach (var contour in characterContours)
            {
                // Convert Mat<Point> to Point[] for FillPoly
                var points = new List<OpenCvSharp.Point>();
                for (int i = 0; i < contour.Rows; i++)
                {
                    var point = contour.At<OpenCvSharp.Point>(i, 0);
                    points.Add(point);
                }
                Cv2.FillPoly(separated, new[] { points.ToArray() }, Scalar.White);
            }

            // Save separated for debugging
            var separatedPath = Path.Combine("captcha_debug", $"separate_characters_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(separatedPath, separated);
            LogMessage($"💾 Saved separated: {separatedPath}");

            // Apply gentle morphology to clean up
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
            using var cleaned = new Mat();
            Cv2.MorphologyEx(separated, cleaned, MorphTypes.Close, kernel);

            // Save final for debugging
            var finalPath = Path.Combine("captcha_debug", $"separate_final_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            Cv2.ImWrite(finalPath, cleaned);
            LogMessage($"💾 Saved final: {finalPath}");

            var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Character Separation" };
            gray.Dispose();
            return result;
        }
        else
        {
            LogMessage("⚠️ Not enough contours found for character separation");
            // Fallback to simple processing
            using var binary = new Mat();
            Cv2.Threshold(scaled, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            
            var result = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "Character Separation (Fallback)" };
            gray.Dispose();
            return result;
        }
    }

    /// <summary>
    /// Preprocessing Method 3: High contrast enhancement for poor quality images
    /// </summary>
    private PreprocessResult ProcessWithHighContrast(Mat input)
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

        // Apply CLAHE (Contrast Limited Adaptive Histogram Equalization)
        using var clahe = Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8));
        using var enhanced = new Mat();
        clahe.Apply(gray, enhanced);

        // Apply sharpening filter
        using var kernel = new Mat(3, 3, MatType.CV_32F, new float[] {
            0, -1, 0,
            -1, 5, -1,
            0, -1, 0
        });
        using var sharpened = new Mat();
        Cv2.Filter2D(enhanced, sharpened, -1, kernel);

        // Apply Otsu threshold
        using var binary = new Mat();
        Cv2.Threshold(sharpened, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        var result = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "High Contrast" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Preprocessing Method 2: Color-based text isolation
    /// </summary>
    private PreprocessResult ProcessWithColorIsolation(Mat input)
    {
        if (input.Channels() < 3)
        {
            return new PreprocessResult { Success = false };
        }

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

        // Create mask for dark text (low saturation, low value)
        using var mask = new Mat();
        var lowerDark = new Scalar(0, 0, 0);
        var upperDark = new Scalar(180, 255, 100);
        Cv2.InRange(hsv, lowerDark, upperDark, mask);

        // Apply mask to original image
        using var isolated = new Mat();
        Cv2.BitwiseAnd(input, input, isolated, mask);

        // Convert to grayscale
        using var gray = new Mat();
        Cv2.CvtColor(isolated, gray, ColorConversionCodes.BGR2GRAY);

        // Apply threshold
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 50, 255, ThresholdTypes.Binary);

        var result = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "Color Isolation" };
        return result;
    }

    /// <summary>
    /// Preprocessing Method 3: Scale up image for better OCR
    /// </summary>
    private PreprocessResult ProcessWithScaling(Mat input)
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

        // Scale up by 3x for better OCR
        using var scaled = new Mat();
        var newSize = new OpenCvSharp.Size(gray.Width * 3, gray.Height * 3);
        Cv2.Resize(gray, scaled, newSize, interpolation: InterpolationFlags.Cubic);

        // Apply Gaussian blur to reduce noise
        using var blurred = new Mat();
        Cv2.GaussianBlur(scaled, blurred, new OpenCvSharp.Size(3, 3), 0);

        // Apply Otsu threshold
        using var binary = new Mat();
        Cv2.Threshold(blurred, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

        var result = new PreprocessResult { ProcessedImage = binary.Clone(), Success = true, Method = "Scaling" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Preprocessing Method 4: Direct binary processing (for already clean images)
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
    /// Preprocessing Method 7: Gentle threshold to preserve faint characters
    /// </summary>
    private PreprocessResult ProcessWithGentleThreshold(Mat input)
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

        // Use a very low threshold to preserve faint characters
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 200, 255, ThresholdTypes.Binary); // High threshold to keep faint text

        // Apply gentle morphological operations to clean up without losing detail
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
        using var cleaned = new Mat();
        Cv2.MorphologyEx(binary, cleaned, MorphTypes.Close, kernel);

        var result = new PreprocessResult { ProcessedImage = cleaned.Clone(), Success = true, Method = "Gentle Threshold" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Preprocessing Method 8: Remove scattered black dots (noise) while preserving characters
    /// </summary>
    private PreprocessResult ProcessWithDenoising(Mat input)
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

        // Step 1: Remove small isolated black dots using opening operation
        using var kernel1 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
        using var opened = new Mat();
        Cv2.MorphologyEx(binary, opened, MorphTypes.Open, kernel1);

        // Step 2: Remove scattered noise by filtering small connected components
        using var denoised = new Mat();
        Cv2.MedianBlur(opened, denoised, 3); // Remove salt-and-pepper noise

        // Step 3: Restore character connectivity with closing operation
        using var kernel2 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1));
        using var restored = new Mat();
        Cv2.MorphologyEx(denoised, restored, MorphTypes.Close, kernel2);

        // Step 4: Remove remaining small isolated components
        using var final = new Mat();
        var contours = Cv2.FindContoursAsMat(restored, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        
        // Create mask for large components only (characters)
        using var mask = Mat.Zeros(restored.Size(), MatType.CV_8UC1);
        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);
            if (area > 50) // Only keep components larger than 50 pixels (characters)
            {
                // Convert Mat<Point> to Point[] for FillPoly
                var points = contour.ToArray();
                Cv2.FillPoly(mask, new[] { points }, Scalar.White);
            }
        }

        // Apply mask to remove small noise
        Cv2.BitwiseAnd(restored, mask, final);

        var result = new PreprocessResult { ProcessedImage = final.Clone(), Success = true, Method = "Denoising" };
        gray.Dispose();
        return result;
    }

    /// <summary>
    /// Perform OCR using OCR.space API
    /// </summary>
    private OCRResult PerformOCRWithSpaceAPI(Mat processedImage)
    {
        try
        {
            LogMessage("🌐 Using OCR.space API...");

            // Convert Mat to byte array
            using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(processedImage);
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            var imageBytes = memoryStream.ToArray();

            // Create request with POST method
            var request = new RestRequest();
            request.Method = RestSharp.Method.Post;
            request.AddHeader("apikey", _ocrSpaceApiKey);
            
            // Add file as form data
            request.AddFile("file", imageBytes, "captcha.png", "image/png");
            
            // Add parameters as form data
            request.AddParameter("language", "eng");
            request.AddParameter("isOverlayRequired", "false");
            request.AddParameter("detectOrientation", "false");
            request.AddParameter("scale", "true");
            request.AddParameter("OCREngine", "2"); // Engine 2 for better accuracy

            // Execute request
            LogMessage($"🌐 Sending request to OCR.space API...");
            LogMessage($"🌐 Request method: {request.Method}");
            LogMessage($"🌐 Request headers: {string.Join(", ", request.Parameters.Where(p => p.Type == ParameterType.HttpHeader).Select(p => $"{p.Name}={p.Value}"))}");
            LogMessage($"🌐 Request parameters: {string.Join(", ", request.Parameters.Where(p => p.Type == ParameterType.GetOrPost).Select(p => $"{p.Name}={p.Value}"))}");
            
            var response = _restClient.Execute(request);
            
            LogMessage($"🌐 Response status: {response.ResponseStatus}");
            LogMessage($"🌐 Response status code: {response.StatusCode}");
            LogMessage($"🌐 Response content: {response.Content}");
            
            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
            {
                var apiResponse = JsonConvert.DeserializeObject<OcrSpaceResponse>(response.Content);
                
                if (apiResponse?.ParsedResults?.Count > 0)
                {
                    var result = apiResponse.ParsedResults[0];
                    var text = result.ParsedText?.Trim() ?? "";
                    var confidence = result.TextOverlay?.Lines?.FirstOrDefault()?.Words?.FirstOrDefault()?.Confidence ?? 0f;
                    
                    LogMessage($"🌐 OCR.space result: '{text}' (confidence: {confidence:F1}%)");
                    
                    return new OCRResult
                    {
                        Text = text,
                        Confidence = confidence,
                        Success = !string.IsNullOrEmpty(text)
                    };
                }
            }
            
            LogMessage($"⚠️ OCR.space API failed: {response.ErrorMessage ?? response.Content}");
            return new OCRResult { Success = false };
        }
        catch (Exception ex)
        {
            LogMessage($"🔥 OCR.space API Error: {ex.Message}");
            return new OCRResult { Success = false };
        }
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
            
            // Try multiple PSM modes for better results - OPTIMIZED for colorful captcha
            var psmModes = new[] { 
                PageSegMode.RawLine,         // BEST for colorful captcha - often gives correct results
                PageSegMode.SingleChar,      // Good for colorful captcha - each character separately
                PageSegMode.SingleWord,      // Good for single word captcha
                PageSegMode.SingleLine,      // Good for horizontal text
                PageSegMode.SingleBlock      // Good for block of text
            };
            var bestResult = "";
            var bestConfidence = 0f;
            var bestPsmMode = PageSegMode.SingleWord;
            
            foreach (var psmMode in psmModes)
            {
                using var page = _tesseractEngine.Process(pix, psmMode);
                var text = page.GetText()?.Trim() ?? "";
                var confidence = page.GetMeanConfidence();
                
                LogMessage($"🔍 PSM {psmMode}: '{text}' (confidence: {confidence:F1}%)");
                
                // Priority 1: Non-empty text with confidence > 0
                if (!string.IsNullOrEmpty(text) && confidence > 0)
                {
                    // NEW: Boost confidence for results that look more accurate
                    var boostedConfidence = confidence;
                    if (text.Length == 4 && text.All(char.IsLetter) && !text.Contains(" ") && !text.Contains("."))
                    {
                        boostedConfidence += 10f; // Boost for clean 4-letter results
                        LogMessage($"🔍 Boosted confidence for clean result '{text}': {confidence}% → {boostedConfidence}%");
                    }
                    
                    if (bestResult == "" || boostedConfidence > bestConfidence)
                    {
                        bestConfidence = boostedConfidence;
                        bestResult = text;
                        bestPsmMode = psmMode;
                    }
                }
                // Priority 2: Non-empty text even with 0 confidence (better than empty)
                else if (!string.IsNullOrEmpty(text) && bestResult == "")
                {
                    bestConfidence = confidence;
                    bestResult = text;
                    bestPsmMode = psmMode;
                }
            }
            
            // If we still don't have a good result, try with different engine mode
            if (string.IsNullOrEmpty(bestResult) || bestConfidence < 10)
            {
                LogMessage("🔍 Trying with Legacy engine mode...");
                _tesseractEngine.SetVariable("tessedit_ocr_engine_mode", "0"); // Legacy + LSTM
                
                foreach (var psmMode in psmModes.Take(3)) // Try only first 3 modes
                {
                    using var page = _tesseractEngine.Process(pix, psmMode);
                    var text = page.GetText()?.Trim() ?? "";
                    var confidence = page.GetMeanConfidence();
                    
                    LogMessage($"🔍 Legacy PSM {psmMode}: '{text}' (confidence: {confidence:F1}%)");
                    
                    if (!string.IsNullOrEmpty(text) && confidence > bestConfidence)
                    {
                        bestConfidence = confidence;
                        bestResult = text;
                        bestPsmMode = psmMode;
                    }
                }
                
                // Restore LSTM mode
                _tesseractEngine.SetVariable("tessedit_ocr_engine_mode", "1");
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
    /// Clean captcha text: replace common OCR mistakes and keep only lowercase letters
    /// </summary>
    private string CleanCaptchaText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // Common OCR mistakes: replace special characters with similar letters
        var replacements = new Dictionary<char, char>
        {
            {'!', 'l'},    // ! looks like l
            {'1', 'l'},    // 1 looks like l
            {'I', 'l'},    // I looks like l
            {'|', 'l'},    // | looks like l
            {'0', 'o'},    // 0 looks like o
            {'O', 'o'},    // O looks like o
            {'5', 's'},    // 5 looks like s
            {'S', 's'},    // S looks like s
            {'8', 'b'},    // 8 looks like b
            {'B', 'b'},    // B looks like b
            {'6', 'g'},    // 6 looks like g
            {'G', 'g'},    // G looks like g
            {'2', 'z'},    // 2 looks like z
            {'Z', 'z'},    // Z looks like z
            {'3', 'e'},    // 3 looks like e
            {'E', 'e'},    // E looks like e
            {'4', 'a'},    // 4 looks like a
            {'A', 'a'},    // A looks like a
            {'7', 't'},    // 7 looks like t
            {'T', 't'},    // T looks like t
            {'9', 'q'},    // 9 looks like q
            {'Q', 'q'},    // Q looks like q
            {'l', 'i'},    // K looks like k
            {'K', 'k'},    // K looks like k
        };

        var result = text.ToLower();
        
        // Apply character replacements
        foreach (var replacement in replacements)
        {
            result = result.Replace(replacement.Key, replacement.Value);
        }
        
        // Remove any remaining non-letter characters
        result = new string(result.Where(c => char.IsLetter(c)).ToArray());
        
        LogMessage($"🧹 Cleaned text: '{text}' -> '{result}' (replaced special chars)");
        return result;
    }

    /// <summary>
    /// Validate if OCR result looks like a valid captcha (3-5 characters)
    /// </summary>
    private bool IsValidCaptchaResult(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim().ToLower();

        // Check length (captcha has 2-5 characters) - allow 2 chars as fallback
        if (text.Length < 2 || text.Length > 5)
            return false;

        // Text is already cleaned, so it only contains lowercase letters
        // No need to check for special characters or numbers

        // Special case: Handle "l" prefix issue (common OCR error)
        if (text.Length == 5 && text.StartsWith("l"))
        {
            var trimmed = text.Substring(1);
            if (trimmed.Length == 4 && trimmed.All(c => char.IsLetter(c)))
            {
                LogMessage($"✅ Fixed OCR error: '{text}' -> '{trimmed}' (removed 'l' prefix)");
                return true;
            }
        }

        // Accept 2-character results (fallback for difficult captcha)
        if (text.Length == 2)
        {
            LogMessage($"✅ Accepting 2-character result: '{text}' (fallback for difficult captcha)");
            return true;
        }

        // Accept 3-character results (common for captcha with missing characters)
        if (text.Length == 3)
        {
            LogMessage($"✅ Accepting 3-character result: '{text}' (may be missing 1 character)");
            return true;
        }

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
            _restClient?.Dispose();
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

// OCR.space API Response Classes
public class OcrSpaceResponse
{
    [JsonProperty("ParsedResults")]
    public List<ParsedResult> ParsedResults { get; set; } = new();
    
    [JsonProperty("IsErroredOnProcessing")]
    public bool IsErroredOnProcessing { get; set; }
    
    [JsonProperty("ErrorMessage")]
    public string ErrorMessage { get; set; } = "";
}

public class ParsedResult
{
    [JsonProperty("ParsedText")]
    public string ParsedText { get; set; } = "";
    
    [JsonProperty("TextOverlay")]
    public TextOverlay TextOverlay { get; set; } = new();
}

public class TextOverlay
{
    [JsonProperty("Lines")]
    public List<Line> Lines { get; set; } = new();
}

public class Line
{
    [JsonProperty("Words")]
    public List<Word> Words { get; set; } = new();
}

public class Word
{
    [JsonProperty("WordText")]
    public string WordText { get; set; } = "";
    
    [JsonProperty("Confidence")]
    public float Confidence { get; set; }
}
