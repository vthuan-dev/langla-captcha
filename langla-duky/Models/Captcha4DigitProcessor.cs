using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;
using System.Diagnostics;

namespace langla_duky.Models
{
    public class Captcha4DigitProcessor
    {
        private readonly string _tessDataPath;

        public Captcha4DigitProcessor(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
        }

        public string ProcessCaptcha4Digits(Bitmap captchaImage)
        {
            if (captchaImage == null)
            {
                Debug.WriteLine("Input captcha image is null.");
                return "";
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Convert Bitmap to Mat
                using var matColor = BitmapConverter.ToMat(captchaImage);
                using var matGray = new Mat();
                using var matBinary = new Mat();

                // Step 1: Convert to grayscale
                Cv2.CvtColor(matColor, matGray, ColorConversionCodes.BGR2GRAY);
                Debug.WriteLine("Converted to grayscale.");

                // Step 2: Apply threshold to create a binary image
                Cv2.Threshold(matGray, matBinary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                Debug.WriteLine("Applied Otsu threshold.");

                // Optional Step: Upscale for better OCR
                using var matUpscaled = new Mat();
                Cv2.Resize(matBinary, matUpscaled, new OpenCvSharp.Size(matBinary.Width * 2, matBinary.Height * 2), 0, 0, InterpolationFlags.Cubic);
                Debug.WriteLine("Upscaled image by 2x.");

                // Tesseract OCR Processing
                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                
                // Whitelist for digits 0-9
                engine.SetVariable("tessedit_char_whitelist", "0123456789");
                
                // Treat the image as a single word
                engine.SetVariable("tessedit_pageseg_mode", "8");

                using var pix = PixConverter.ToPix(matUpscaled.ToBitmap());
                using var page = engine.Process(pix);
                
                string result = page.GetText().Trim();
                Debug.WriteLine($"Raw Tesseract result: '{result}'");

                // Clean result: keep only digits
                result = Regex.Replace(result, @"[^0-9]", "");
                Debug.WriteLine($"Cleaned result: '{result}'");

                // Validate the result
                if (result.Length != 4)
                {
                    Debug.WriteLine($"Validation failed: Length is {result.Length}, expected 4.");
                    return "";
                }

                if (!Regex.IsMatch(result, @"^\d{{4}}$"))
                {
                    Debug.WriteLine("Validation failed: Result does not match ^\\d{4}$");
                    return "";
                }
                
                stopwatch.Stop();
                Debug.WriteLine($"Captcha processed successfully in {stopwatch.ElapsedMilliseconds} ms. Result: '{result}'");

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred during captcha processing: {ex.Message}");
                stopwatch.Stop();
                return "";
            }
        }
    }
}
