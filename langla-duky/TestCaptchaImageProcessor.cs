using System;
using System.Drawing;
using langla_duky.Models;

public class TestCaptchaImageProcessor
{
    public static void RunTest()
    {
        Console.WriteLine("Running Captcha Image Processor Test...");

        // Ensure you have a test image named 'captcha_crop_test.png' in the same directory 
        // as the executable, or provide the correct path.
        string imagePath = "captcha_crop_test.png";

        if (!System.IO.File.Exists(imagePath))
        {
            Console.WriteLine($"Test image not found at: {imagePath}");
            return;
        }

        var processor = new CaptchaImageProcessor("./tessdata");

        using (var testImage = new Bitmap(imagePath))
        {
            string result = processor.ProcessCaptchaImage(testImage);

            Console.WriteLine($"OCR Result: '{result}'");

            bool isValid = result.Length == 4 && result.All(char.IsLetter);
            Console.WriteLine($"Is Valid (4 letters): {isValid}");

            if (isValid)
            {
                Console.WriteLine("Test PASSED!");
            }
            else
            { 
                Console.WriteLine("Test FAILED.");
            }
        }
    }
}
