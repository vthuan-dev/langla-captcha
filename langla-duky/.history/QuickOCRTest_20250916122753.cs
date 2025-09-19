using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using langla_duky.Models;
using Tesseract;

namespace langla_duky
{
    public class QuickOCRTest
    {
        public static void RunQuickTest()
        {
            Console.WriteLine("--- Chạy Quick OCR Test ---");

            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Thư mục gốc: {baseDirectory}");

                // Tìm hình ảnh test
                string imagePath = Path.Combine(baseDirectory, "test_image", "ocr-1.png");
                if (!File.Exists(imagePath))
                {
                    Console.WriteLine($"Lỗi: Không tìm thấy hình ảnh tại '{imagePath}'");
                    return;
                }
                Console.WriteLine($"Đã tìm thấy hình ảnh: {imagePath}");

                // Tìm thư mục tessdata
                string tessdataPath = Path.Combine(baseDirectory, "tessdata");
                if (!Directory.Exists(tessdataPath))
                {
                    Console.WriteLine($"Lỗi: Không tìm thấy thư mục tessdata tại '{tessdataPath}'");
                    return;
                }
                Console.WriteLine($"Đã tìm thấy tessdata: {tessdataPath}");

                // Chạy OCR
                using (var image = new Bitmap(imagePath))
                {
                    Console.WriteLine($"Kích thước ảnh: {image.Width}x{image.Height}");
                    
                    using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
                    {
                        using (var pix = PixConverter.ToPix(image))
                        {
                            using (var page = engine.Process(pix))
                            {
                                string result = page.GetText().Trim();
                                Console.WriteLine($"Kết quả OCR: '{result}'");

                                if (string.IsNullOrWhiteSpace(result))
                                {
                                    MessageBox.Show("Lỗi: OCR không nhận dạng được văn bản.", "Lỗi OCR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                else
                                {
                                    MessageBox.Show($"Kết quả OCR:\n'{result}'", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi nghiêm trọng: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Lỗi nghiêm trọng trong quá trình OCR: {ex.Message}", "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
