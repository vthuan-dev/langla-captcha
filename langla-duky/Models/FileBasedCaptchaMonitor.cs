using System;
using System.IO;
using System.Threading;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    public class FileBasedCaptchaMonitor : IDisposable
    {
        private readonly string _imageFolderPath;
        private readonly AutoCaptchaROIDetector _roiDetector;
        private readonly GameCaptchaSolver _captchaSolver;
        private readonly bool _enableDebugOutput;
        
        private System.Threading.Timer _monitoringTimer;
        private bool _isMonitoring = false;
        private int _monitoringInterval = 2000; // 2 seconds
        private readonly object _lockObject = new object();
        
        // Events
#pragma warning disable CS0067 // Event is never used
        public event EventHandler<CaptchaDetectedEventArgs>? CaptchaDetected;
#pragma warning restore CS0067
        public event EventHandler<CaptchaSolvedEventArgs>? CaptchaSolved;
        public event EventHandler<CaptchaFailedEventArgs>? CaptchaFailed;
        public event EventHandler<string>? StatusChanged;

        public FileBasedCaptchaMonitor(string imageFolderPath = "capture-compare", bool enableDebugOutput = true)
        {
            _imageFolderPath = imageFolderPath;
            _enableDebugOutput = enableDebugOutput;
            _roiDetector = new AutoCaptchaROIDetector(enableDebugOutput);
            _captchaSolver = new GameCaptchaSolver();
            
            _monitoringTimer = new System.Threading.Timer(MonitorCallback, null, Timeout.Infinite, Timeout.Infinite);
            
            // Ensure folder exists
            Directory.CreateDirectory(_imageFolderPath);
        }

        public void StartMonitoring(int intervalMs = 2000)
        {
            lock (_lockObject)
            {
                if (_isMonitoring) return;
                
                _monitoringInterval = intervalMs;
                _isMonitoring = true;
                _monitoringTimer.Change(0, _monitoringInterval);
                
                OnStatusChanged($"🔄 File-based monitoring started (checking folder: {_imageFolderPath})");
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring) return;
                
                _isMonitoring = false;
                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                
                OnStatusChanged("⏹️ File-based monitoring stopped");
            }
        }

        private void MonitorCallback(object? state)
        {
            if (!_isMonitoring) return;

            try
            {
                // Check for new images in the folder
                var imageFiles = GetImageFiles();
                
                if (imageFiles.Length == 0)
                {
                    if (_enableDebugOutput)
                        Console.WriteLine($"📂 No images found in folder: {_imageFolderPath}");
                    return;
                }

                // Process the most recent image
                var latestImage = GetLatestImageFile(imageFiles);
                if (latestImage == null)
                {
                    if (_enableDebugOutput)
                        Console.WriteLine("📂 No valid image files found");
                    return;
                }

                if (_enableDebugOutput)
                    Console.WriteLine($"📸 Processing image: {Path.GetFileName(latestImage)}");

                // Load image from file
                using var screenshot = LoadImageFromFile(latestImage);
                if (screenshot == null || screenshot.Empty())
                {
                    if (_enableDebugOutput)
                        Console.WriteLine($"❌ Failed to load image: {latestImage}");
                    return;
                }

                // Detect captcha region (FAST - 2-3 seconds max)
                var detectionResult = _roiDetector.DetectCaptchaRegion(screenshot);
                
                if (!detectionResult.Success)
                {
                    if (_enableDebugOutput)
                        Console.WriteLine($"🔍 No captcha detected in {Path.GetFileName(latestImage)}");
                    return;
                }

                if (_enableDebugOutput)
                    Console.WriteLine($"🎯 Captcha detected! Region: {detectionResult.Region}, Confidence: {detectionResult.Confidence:F1}%");

                // Trigger captcha detected event
                OnCaptchaDetected(new CaptchaDetectedEventArgs
                {
                    Region = detectionResult.Region,
                    Confidence = detectionResult.Confidence,
                    Method = detectionResult.Method
                });

                // Crop captcha region
                using var captchaImage = new Mat(screenshot, new OpenCvSharp.Rect(
                    detectionResult.Region.X, 
                    detectionResult.Region.Y, 
                    detectionResult.Region.Width, 
                    detectionResult.Region.Height));

                // Solve captcha
                var solveResult = _captchaSolver.SolveCaptcha(captchaImage);
                
                if (solveResult.Success)
                {
                    if (_enableDebugOutput)
                        Console.WriteLine($"✅ Captcha solved: '{solveResult.Text}' (Confidence: {solveResult.Confidence:F1}%)");

                    // Trigger captcha solved event
                    OnCaptchaSolved(new CaptchaSolvedEventArgs
                    {
                        CaptchaText = solveResult.Text,
                        Confidence = solveResult.Confidence,
                        Method = solveResult.Method,
                        ProcessingTime = TimeSpan.Zero
                    });
                }
                else
                {
                    if (_enableDebugOutput)
                        Console.WriteLine($"❌ Failed to solve captcha from {Path.GetFileName(latestImage)}");

                    // Trigger captcha failed event
                    OnCaptchaFailed(new CaptchaFailedEventArgs
                    {
                        Error = "Failed to solve captcha",
                        DetectionConfidence = detectionResult.Confidence
                    });
                }

                // Move processed image to avoid reprocessing
                MoveProcessedImage(latestImage);
            }
            catch (Exception ex)
            {
                if (_enableDebugOutput)
                    Console.WriteLine($"❌ Error in file monitoring: {ex.Message}");
                
                OnStatusChanged($"❌ Monitoring error: {ex.Message}");
            }
        }

        private string[] GetImageFiles()
        {
            try
            {
                var extensions = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" };
                var files = new List<string>();
                
                foreach (var ext in extensions)
                {
                    files.AddRange(Directory.GetFiles(_imageFolderPath, ext, SearchOption.TopDirectoryOnly));
                }
                
                return files.ToArray();
            }
            catch (Exception ex)
            {
                if (_enableDebugOutput)
                    Console.WriteLine($"❌ Error getting image files: {ex.Message}");
                return new string[0];
            }
        }

        private string? GetLatestImageFile(string[] imageFiles)
        {
            if (imageFiles.Length == 0) return null;
            
            return imageFiles
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .FirstOrDefault();
        }

        private Mat? LoadImageFromFile(string imagePath)
        {
            try
            {
                // Load using OpenCV
                var mat = Cv2.ImRead(imagePath, ImreadModes.Color);
                
                if (mat != null && !mat.Empty())
                {
                    return mat;
                }
                
                // Fallback: Load using System.Drawing and convert
                using var bitmap = new Bitmap(imagePath);
                return bitmap.ToMat();
            }
            catch (Exception ex)
            {
                if (_enableDebugOutput)
                    Console.WriteLine($"❌ Error loading image {imagePath}: {ex.Message}");
                return null;
            }
        }

        private void MoveProcessedImage(string imagePath)
        {
            try
            {
                var processedFolder = Path.Combine(_imageFolderPath, "processed");
                Directory.CreateDirectory(processedFolder);
                
                var fileName = Path.GetFileName(imagePath);
                var newPath = Path.Combine(processedFolder, fileName);
                
                // If file already exists, add timestamp
                if (File.Exists(newPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    fileName = $"{nameWithoutExt}_{timestamp}{extension}";
                    newPath = Path.Combine(processedFolder, fileName);
                }
                
                File.Move(imagePath, newPath);
                
                if (_enableDebugOutput)
                    Console.WriteLine($"📁 Moved processed image to: {newPath}");
            }
            catch (Exception ex)
            {
                if (_enableDebugOutput)
                    Console.WriteLine($"❌ Error moving processed image: {ex.Message}");
            }
        }

        // Event handlers
        protected virtual void OnCaptchaDetected(CaptchaDetectedEventArgs e)
        {
            CaptchaDetected?.Invoke(this, e);
        }

        protected virtual void OnCaptchaSolved(CaptchaSolvedEventArgs e)
        {
            CaptchaSolved?.Invoke(this, e);
        }

        protected virtual void OnCaptchaFailed(CaptchaFailedEventArgs e)
        {
            CaptchaFailed?.Invoke(this, e);
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
            _roiDetector?.Dispose();
            _captchaSolver?.Dispose();
        }
    }
}
