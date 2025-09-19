using System;
using System.Drawing;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;

namespace langla_duky
{
    public static class TestOCRDirect
    {
        public static void RunDirectTest()
        {
            try
            {
                System.Console.WriteLine("🧪 Bắt đầu test OCR API trực tiếp...");
                
                // Tạo ảnh test đơn giản
                using (var testImage = new Bitmap(200, 50))
                {
                    using (var g = Graphics.FromImage(testImage))
                    {
                        g.Clear(Color.White);
                        g.DrawString("TEST", new Font("Arial", 20, FontStyle.Bold), Brushes.Black, 10, 10);
                    }
                    
                    System.Console.WriteLine("✅ Đã tạo ảnh test");

                    // OpenCV binarize + Tesseract trực tiếp
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
                    using var pix = Pix.LoadFromMemory(ms2.ToArray());
                    using var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
                    engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                    using var page = engine.Process(pix);
                    string result = page.GetText().Trim();
                    System.Console.WriteLine($"📝 Kết quả OCR (local): '{result}'");
                }
                
                System.Console.WriteLine("🎉 Test OCR API hoàn thành");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Lỗi trong test OCR API: {ex.Message}");
                System.Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }
    }
}