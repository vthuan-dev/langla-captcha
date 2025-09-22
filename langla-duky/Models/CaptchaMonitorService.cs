using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    public class CaptchaMonitorService
    {
        private readonly AutoCaptchaROIDetector _roiDetector;
        private readonly GameCaptchaSolver _captchaSolver;
        private readonly IntPtr _gameWindowHandle;
        private readonly System.Threading.Timer _monitoringTimer;
        private readonly object _lockObject = new object();
        
        private bool _isMonitoring = false;
        private bool _isProcessingCaptcha = false;
        private DateTime _lastCaptchaDetection = DateTime.MinValue;
        private int _monitoringInterval = 2000; // 2 seconds
        private double _minConfidenceThreshold = 50.0; // Minimum confidence to trigger captcha processing
        
        // Events
#pragma warning disable CS0067 // Event is never used
        public event EventHandler<CaptchaDetectedEventArgs>? CaptchaDetected;
#pragma warning restore CS0067
        public event EventHandler<CaptchaSolvedEventArgs>? CaptchaSolved;
        public event EventHandler<CaptchaFailedEventArgs>? CaptchaFailed;
        public event EventHandler<string>? StatusChanged;

        public CaptchaMonitorService(IntPtr gameWindowHandle, bool enableDebugOutput = true)
        {
            _gameWindowHandle = gameWindowHandle;
            _roiDetector = new AutoCaptchaROIDetector(enableDebugOutput);
            _captchaSolver = new GameCaptchaSolver();
            
            _monitoringTimer = new System.Threading.Timer(MonitorCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartMonitoring(int intervalMs = 2000)
        {
            lock (_lockObject)
            {
                if (_isMonitoring) return;
                
                _monitoringInterval = intervalMs;
                _isMonitoring = true;
                _monitoringTimer.Change(0, _monitoringInterval);
                
                OnStatusChanged("🔄 Captcha monitoring started");
            }
        }

        public void StopMonitoring()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring) return;
                
                _isMonitoring = false;
                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                
                OnStatusChanged("⏹️ Captcha monitoring stopped");
            }
        }

        private void MonitorCallback(object? state)
        {
            if (!_isMonitoring || _isProcessingCaptcha) return;

            try
            {
                // Skip if we just processed a captcha recently (avoid duplicate processing)
                if (DateTime.Now - _lastCaptchaDetection < TimeSpan.FromSeconds(10))
                {
                    return;
                }

                // Capture game window screenshot
                var screenshot = CaptureGameWindow();
                if (screenshot == null || screenshot.Empty())
                {
                    OnStatusChanged("⚠️ Failed to capture game window");
                    return;
                }

                // Detect captcha region
                var detectionResult = _roiDetector.DetectCaptchaRegion(screenshot);
                
                if (detectionResult.Success && detectionResult.Confidence >= _minConfidenceThreshold)
                {
                    OnStatusChanged($"🎯 Captcha detected! Confidence: {detectionResult.Confidence:F1}%, Method: {detectionResult.Method}");
                    
                    // Process captcha asynchronously
                    _ = Task.Run(() => ProcessDetectedCaptcha(screenshot, detectionResult));
                }
                else
                {
                    // Optional: Show status for debugging
                    if (detectionResult.Success)
                    {
                        OnStatusChanged($"🔍 Low confidence detection: {detectionResult.Confidence:F1}% (threshold: {_minConfidenceThreshold:F1}%)");
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"❌ Monitoring error: {ex.Message}");
            }
        }

        private Mat? CaptureGameWindow()
        {
            try
            {
                // Get window rectangle
                var windowRect = new RECT();
                if (!Win32.GetWindowRect(_gameWindowHandle, out windowRect))
                {
                    return null;
                }

                // Create bitmap
                var width = windowRect.Right - windowRect.Left;
                var height = windowRect.Bottom - windowRect.Top;
                
                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                
                // Capture window
                var hdcSrc = Win32.GetWindowDC(_gameWindowHandle);
                var hdcDest = graphics.GetHdc();
                
                Win32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, Win32.SRCCOPY);
                
                graphics.ReleaseHdc(hdcDest);
                Win32.ReleaseDC(_gameWindowHandle, hdcSrc);
                
                // Convert to OpenCV Mat
                return bitmap.ToMat();
            }
            catch (Exception ex)
            {
                OnStatusChanged($"❌ Capture error: {ex.Message}");
                return null;
            }
        }

        private async Task ProcessDetectedCaptcha(Mat screenshot, ROIDetectionResult detectionResult)
        {
            lock (_lockObject)
            {
                if (_isProcessingCaptcha) return;
                _isProcessingCaptcha = true;
                _lastCaptchaDetection = DateTime.Now;
            }

            try
            {
                OnStatusChanged("🔍 Processing detected captcha...");
                
                // Crop captcha region
                var captchaRegion = new OpenCvSharp.Rect(
                    detectionResult.Region.X, 
                    detectionResult.Region.Y, 
                    detectionResult.Region.Width, 
                    detectionResult.Region.Height
                );
                
                using var captchaImage = new Mat(screenshot, captchaRegion);
                
                // Solve captcha
                var captchaResult = _captchaSolver.SolveCaptcha(captchaImage);
                
                if (captchaResult.Success && !string.IsNullOrEmpty(captchaResult.Text))
                {
                    OnStatusChanged($"✅ Captcha solved: '{captchaResult.Text}' (Confidence: {captchaResult.Confidence:F1}%)");
                    
                    // Auto-fill captcha
                    await AutoFillCaptcha(captchaResult.Text);
                    
                    // Trigger events
                    CaptchaSolved?.Invoke(this, new CaptchaSolvedEventArgs
                    {
                        CaptchaText = captchaResult.Text,
                        Confidence = captchaResult.Confidence,
                        Method = captchaResult.Method,
                        ProcessingTime = captchaResult.ProcessingTime
                    });
                }
                else
                {
                    OnStatusChanged($"❌ Failed to solve captcha: {captchaResult.Error}");
                    
                    CaptchaFailed?.Invoke(this, new CaptchaFailedEventArgs
                    {
                        Error = captchaResult.Error,
                        DetectionConfidence = detectionResult.Confidence
                    });
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged($"❌ Captcha processing error: {ex.Message}");
                
                CaptchaFailed?.Invoke(this, new CaptchaFailedEventArgs
                {
                    Error = ex.Message,
                    DetectionConfidence = detectionResult.Confidence
                });
            }
            finally
            {
                lock (_lockObject)
                {
                    _isProcessingCaptcha = false;
                }
            }
        }

        private async Task AutoFillCaptcha(string captchaText)
        {
            try
            {
                OnStatusChanged($"⌨️ Auto-filling captcha: '{captchaText}'");
                
                // Load config to get correct coordinates
                var config = Config.LoadFromFile();
                if (config == null)
                {
                    OnStatusChanged("❌ Failed to load config for auto-fill");
                    return;
                }
                
                // Get input field coordinates from config
                System.Drawing.Point inputPoint;
                System.Drawing.Point confirmPoint;
                
                if (config.UseAbsoluteCoordinates)
                {
                    inputPoint = new System.Drawing.Point(config.InputFieldX, config.InputFieldY);
                    confirmPoint = new System.Drawing.Point(config.ConfirmButtonX, config.ConfirmButtonY);
                }
                else
                {
                    inputPoint = config.InputFieldPosition;
                    confirmPoint = config.ConfirmButtonPosition;
                }
                
                OnStatusChanged($"🎯 Using coordinates: Input=({inputPoint.X},{inputPoint.Y}), Confirm=({confirmPoint.X},{confirmPoint.Y})");
                
                // Set focus to game window first
                Win32.SetForegroundWindow(_gameWindowHandle);
                await Task.Delay(200);
                
                // Click on input field using InputAutomation
                OnStatusChanged($"🖱️ Clicking input field at ({inputPoint.X},{inputPoint.Y})");
                InputAutomation.ClickInWindow(_gameWindowHandle, inputPoint);
                await Task.Delay(config.AutomationSettings.DelayAfterClick);
                
                // Clear existing text and type captcha
                OnStatusChanged($"⌨️ Typing captcha: '{captchaText}'");
                InputAutomation.SendTextToWindow(_gameWindowHandle, captchaText);
                await Task.Delay(config.AutomationSettings.DelayAfterInput);
                
                // Click confirm button instead of pressing Enter (to avoid exiting game)
                OnStatusChanged($"🖱️ Clicking confirm button at ({confirmPoint.X},{confirmPoint.Y})");
                InputAutomation.ClickInWindow(_gameWindowHandle, confirmPoint);
                await Task.Delay(config.AutomationSettings.DelayAfterInput);
                
                OnStatusChanged("✅ Captcha auto-filled and submitted successfully");
            }
            catch (Exception ex)
            {
                OnStatusChanged($"❌ Auto-fill error: {ex.Message}");
            }
        }

        protected virtual void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, status);
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
            _captchaSolver?.Dispose();
        }
    }

    // Event argument classes
    public class CaptchaDetectedEventArgs : EventArgs
    {
        public Rectangle Region { get; set; }
        public double Confidence { get; set; }
        public string Method { get; set; } = "";
    }

    public class CaptchaSolvedEventArgs : EventArgs
    {
        public string CaptchaText { get; set; } = "";
        public float Confidence { get; set; }
        public string Method { get; set; } = "";
        public TimeSpan ProcessingTime { get; set; }
    }

    public class CaptchaFailedEventArgs : EventArgs
    {
        public string Error { get; set; } = "";
        public double DetectionConfidence { get; set; }
    }

    // Win32 API declarations
    public static class Win32
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        public const uint SRCCOPY = 0x00CC0020;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
