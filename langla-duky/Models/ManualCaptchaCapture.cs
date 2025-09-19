using System;
using System.Drawing;
using System.IO;

namespace langla_duky.Models
{
    public class ManualCaptchaCapture
    {
        private readonly Config _config;
        private readonly string _debugOutputPath = "captcha_debug";

        public ManualCaptchaCapture(Config config)
        {
            _config = config;
            if (!Directory.Exists(_debugOutputPath))
            {
                Directory.CreateDirectory(_debugOutputPath);
            }
        }

        public Rectangle CaptchaArea
        {
            get => _config.ManualCaptchaArea;
            private set => _config.ManualCaptchaArea = value;
        }

        public Point InputField
        {
            get => _config.ManualInputField;
            private set => _config.ManualInputField = value;
        }

        public Point ConfirmButton
        {
            get => _config.ManualConfirmButton;
            private set => _config.ManualConfirmButton = value;
        }

        // The GameWindow parameter is kept for context; capture can be based on screen or client coordinates depending on config.
        public Bitmap? CaptureCaptchaArea(GameWindow? gameWindow)
        {
            if (!IsValid())
            {
                Console.WriteLine("Manual capture failed: Captcha area is not valid.");
                return null;
            }
            
            if (gameWindow == null || !gameWindow.IsValid())
            {
                Console.WriteLine("Warning: Game window not found or not running. Attempting capture based on absolute coordinates.");
            }

            try
            {
                Bitmap captchaImage;
                if (_config.UseAbsoluteCoordinates || gameWindow == null || !gameWindow.IsValid())
                {
                    // Interpret ManualCaptchaArea as absolute screen rectangle
                    captchaImage = ScreenCapture.CaptureScreen(CaptchaArea);
                }
                else
                {
                    // Interpret ManualCaptchaArea as client-area rectangle of the game window
                    captchaImage = ScreenCapture.CaptureWindowClientArea(gameWindow.Handle, CaptchaArea);
                }

                // Save debug image
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var debugImagePath = Path.Combine(_debugOutputPath, $"manual_captcha_{timestamp}.png");
                captchaImage?.Save(debugImagePath, System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine($"Saved manual captcha debug image to {debugImagePath}");

                return captchaImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during manual captcha capture: {ex.Message}");
                return null;
            }
        }

        public void SetCaptchaArea(Rectangle area)
        {
            if (area.Width > 0 && area.Height > 0)
            {
                CaptchaArea = area;
                Console.WriteLine($"Manual captcha area set to: {area}");
            }
        }

        public void SetInputField(Point point)
        {
            InputField = point;
            Console.WriteLine($"Manual input field set to: {point}");
        }

        public void SetConfirmButton(Point point)
        { ConfirmButton = point;
            Console.WriteLine($"Manual confirm button set to: {point}");
        }

        public bool IsValid()
        {
            // The area should have a minimum size to be considered valid.
            return CaptchaArea.Width > 10 && CaptchaArea.Height > 10;
        }
    }
}
