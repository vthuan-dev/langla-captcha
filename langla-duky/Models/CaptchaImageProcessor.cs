using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text.RegularExpressions;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace langla_duky.Models
{
    public class CaptchaImageProcessor
    {
        private readonly string _tessDataPath;

        public CaptchaImageProcessor(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
        }

        public string ProcessCaptchaImage(Bitmap captchaImage)
        {
            if (captchaImage == null)
            {
                return "";
            }

            try
            {
                using (var matColor = BitmapConverter.ToMat(captchaImage))
                {
                    using (var matGray = new Mat())
                    {
                        // Step 1: Convert to grayscale
                        Cv2.CvtColor(matColor, matGray, ColorConversionCodes.BGR2GRAY);

                        using (var matBinary = new Mat())
                        {
                            // Step 2: Apply threshold to create a binary image
                            Cv2.Threshold(matGray, matBinary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                            // Optional: Clean noise
                            using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)))
                            {
                                Cv2.MorphologyEx(matBinary, matBinary, MorphTypes.Close, kernel);
                            }
                            
                            // Step 3: Tesseract OCR
                            using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
                            {
                                // Whitelist for 4-letter captchas (no numbers)
                                engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
                                engine.SetVariable("tessedit_pageseg_mode", "8"); // Assume a single word.

                                using (var pix = PixConverter.ToPix(matBinary.ToBitmap()))
                                {
                                    using (var page = engine.Process(pix))
                                    {
                                        string result = page.GetText().Trim();
                                        
                                        // Clean result: keep only letters
                                        result = Regex.Replace(result, @"[^a-zA-Z]", "");

                                        // Step 4: Validate result
                                        if (result.Length == 4 && !result.Contains(" "))
                                        {
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Log exception here if needed
                return "";
            }

            return "";
        }
    }
}
