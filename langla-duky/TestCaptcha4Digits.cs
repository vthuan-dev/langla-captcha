using System;
using System.Drawing;
using System.IO;
using System.Linq;
using langla_duky.Models;

namespace langla_duky.Tests
{
    public class TestCaptcha4Digits
    {
        public static void RunTest()
        {
            Console.WriteLine("--- Running Captcha 4-Digit Processor Test ---");
            
            // Path to the test image
            string imagePath = "captcha_crop_test.png";

            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"ERROR: Test image not found at '{imagePath}'");
                return;
            }

            var processor = new Captcha4DigitProcessor("./tessdata");
            
            try
            {
                // Test with a sample image
                using var testImage = new Bitmap(imagePath);
                Console.WriteLine($"Loaded test image: {imagePath} ({testImage.Width}x{testImage.Height})");

                string result = processor.ProcessCaptcha4Digits(testImage);
                
                bool isValid = result.Length == 4 && result.All(char.IsDigit);

                Console.WriteLine($"Result: '{result}'");
                Console.WriteLine($"Valid: {isValid}");

                if (isValid)
                {
                    Console.WriteLine("SUCCESS: The result is a valid 4-digit string.");
                }
                else
                {
                    Console.WriteLine("FAILURE: The result is not a valid 4-digit string.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An exception occurred during the test: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("--- Test Finished ---");
            }
        }
    }
}
