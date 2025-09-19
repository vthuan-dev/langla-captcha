using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace langla_duky
{
    public class TestOCRSpaceAPI
    {
        public static async Task TestOCRAPI()
        {
            Console.WriteLine("🧪 Testing OCR.space API...");
            
            try
            {
                await Task.Run(() =>
                {
                    using (var testImage = CreateTestImage())
                    {
                        Console.WriteLine($"📸 Test image created: {testImage.Width}x{testImage.Height}");
                        using var ms = new MemoryStream();
                        testImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var mat = Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
                        using var gray = new Mat();
                        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                        using var blur = new Mat();
                        Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(3, 3), 0);
                        using var bin = new Mat();
                        Cv2.Threshold(blur, bin, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                        using var binBmp = bin.ToBitmap();
                        using var ms2 = new MemoryStream();
                        binBmp.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                        using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
                        using var pix = Pix.LoadFromMemory(ms2.ToArray());
                        using var page = engine.Process(pix);
                        var result = page.GetText().Trim();
                        Console.WriteLine($"🔍 OCR Result (local): '{result}'");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OCR API test error: {ex.Message}");
            }
        }
        
        private static Bitmap CreateTestImage()
        {
            // Tạo một ảnh test đơn giản với text "TEST"
            var bitmap = new Bitmap(200, 80);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.DrawString("TEST", new Font("Arial", 24, FontStyle.Bold), Brushes.Black, new PointF(50, 25));
            }
            return bitmap;
        }
    }
}
