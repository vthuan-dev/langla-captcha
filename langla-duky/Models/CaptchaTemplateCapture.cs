using System;
using System.Drawing;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace langla_duky.Models
{
    public class CaptchaTemplateCapture
    {
        private readonly AutoCaptchaROIDetector _roiDetector;
        private readonly bool _enableDebugOutput;
        
        public CaptchaTemplateCapture(bool enableDebugOutput = true)
        {
            _enableDebugOutput = enableDebugOutput;
            _roiDetector = new AutoCaptchaROIDetector(enableDebugOutput);
        }
        
        // Method to capture current captcha and save as template
        public bool CaptureAndSaveTemplate(IntPtr gameWindowHandle)
        {
            try
            {
                Console.WriteLine("📸 Capturing screenshot to detect captcha...");
                
                // Capture screenshot
                using var screenshot = ScreenCapture.CaptureWindow(gameWindowHandle, Rectangle.Empty);
                if (screenshot == null)
                {
                    Console.WriteLine("❌ Failed to capture screenshot");
                    return false;
                }
                
                using var cvScreenshot = screenshot.ToMat();
                if (cvScreenshot.Empty())
                {
                    Console.WriteLine("❌ Captured screenshot is empty");
                    return false;
                }
                
                Console.WriteLine("🔍 Detecting captcha region...");
                
                // Detect captcha region
                var detectionResult = _roiDetector.DetectCaptchaRegion(cvScreenshot);
                
                if (detectionResult.Success)
                {
                    Console.WriteLine($"✅ Captcha detected! Region: {detectionResult.Region}, Confidence: {detectionResult.Confidence:F1}%");
                    Console.WriteLine($"📊 Method: {detectionResult.Method}");
                    
                    // Save as template
                    _roiDetector.SaveCaptchaTemplate(cvScreenshot, detectionResult.Region);
                    
                    Console.WriteLine("💾 Captcha template saved successfully!");
                    Console.WriteLine("🎯 Next time, Image Comparison will be used for faster detection!");
                    
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ No captcha detected: {detectionResult.DebugInfo}");
                    Console.WriteLine("💡 Make sure captcha is visible on screen");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error capturing template: {ex.Message}");
                return false;
            }
        }
        
        // Method to manually select captcha region and save as template
        public bool ManualCaptureAndSaveTemplate(IntPtr gameWindowHandle)
        {
            try
            {
                Console.WriteLine("📸 Capturing screenshot for manual selection...");
                
                // Capture screenshot
                using var screenshot = ScreenCapture.CaptureWindow(gameWindowHandle, Rectangle.Empty);
                if (screenshot == null)
                {
                    Console.WriteLine("❌ Failed to capture screenshot");
                    return false;
                }
                
                using var cvScreenshot = screenshot.ToMat();
                if (cvScreenshot.Empty())
                {
                    Console.WriteLine("❌ Captured screenshot is empty");
                    return false;
                }
                
                // Save screenshot for manual inspection
                var screenshotPath = "captcha_debug/manual_capture.png";
                Cv2.ImWrite(screenshotPath, cvScreenshot);
                Console.WriteLine($"💾 Screenshot saved: {screenshotPath}");
                
                // Show message to user
                var result = MessageBox.Show(
                    "Screenshot saved to captcha_debug/manual_capture.png\n\n" +
                    "Please inspect the image and manually select the captcha region.\n\n" +
                    "Click OK to continue with automatic detection, or Cancel to abort.",
                    "Manual Captcha Template Capture",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Information);
                
                if (result == DialogResult.Cancel)
                {
                    return false;
                }
                
                // Try automatic detection again
                return CaptureAndSaveTemplate(gameWindowHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in manual capture: {ex.Message}");
                return false;
            }
        }
        
        public void Dispose()
        {
            _roiDetector?.Dispose();
        }
    }
}
