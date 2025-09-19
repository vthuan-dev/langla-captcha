using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace langla_duky.Models
{
    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static float GetScaleForWindow(IntPtr hwnd)
        {
            try
            {
                uint dpi = GetDpiForWindow(hwnd);
                return dpi / 96.0f;
            }
            catch
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    return g.DpiX / 96.0f;
                }
            }
        }

        public static Bitmap CaptureWindow(IntPtr windowHandle, Rectangle captureArea)
        {
            float dpiScale = GetScaleForWindow(windowHandle);
            Console.WriteLine($"[Capture] Capturing window {windowHandle} with DPI scale {dpiScale:F2}x");

            IntPtr hdcSrc = GetDC(windowHandle);
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, captureArea.Width, captureArea.Height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, captureArea.Width, captureArea.Height,
                hdcSrc, captureArea.X, captureArea.Y, SRCCOPY);

            Bitmap result = Image.FromHbitmap(hBitmap);

            SelectObject(hdcDest, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            ReleaseDC(windowHandle, hdcSrc);

            return result;
        }

        public static Bitmap? CaptureWindowClientArea(IntPtr windowHandle, Rectangle captureArea)
        {
            if (windowHandle == IntPtr.Zero) return null;

            if (!GetClientRect(windowHandle, out RECT clientRect)) return null;

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;

            if (width <= 0 || height <= 0) return null;

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var gfx = Graphics.FromImage(bmp))
            {
                var hdc = gfx.GetHdc();
                const uint PW_CLIENTONLY = 1;

                bool success = PrintWindow(windowHandle, hdc, PW_CLIENTONLY);
                
                gfx.ReleaseHdc(hdc);

                if (!success)
                {
                    bmp.Dispose();
                    // Fallback to the old method if PrintWindow fails
                    Point topLeft = new Point(clientRect.Left, clientRect.Top);
                    ClientToScreen(windowHandle, ref topLeft);
                    var fallbackBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using (var fallbackGfx = Graphics.FromImage(fallbackBmp))
                    {
                        fallbackGfx.CopyFromScreen(topLeft.X, topLeft.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                    }
                    return fallbackBmp;
                }
            }

            return bmp;
        }

        public static Bitmap CaptureScreen(Rectangle captureArea)
        {
            Bitmap bitmap = new Bitmap(captureArea.Width, captureArea.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(captureArea.Location, Point.Empty, captureArea.Size);
            }
            return bitmap;
        }
        
        public static Bitmap CaptureScreenAbsolute(int leftX, int topY, int rightX, int bottomY)
        {
            int width = rightX - leftX;
            int height = bottomY - topY;
            
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentException("Invalid capture area coordinates");
            }
            
            Size s = new Size(width, height);
            Rectangle rect = new Rectangle(leftX, topY, width, height);

            Bitmap memoryImage = new Bitmap(width, height);
            using (Graphics memoryGraphics = Graphics.FromImage(memoryImage))
            {
                memoryGraphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, s);
            }
            
            return memoryImage;
        }

        public static Bitmap PreprocessCaptchaImage(Bitmap originalImage)
        {
            try
            {
                string debugFolder = "ocr_debug_output";
                if (!Directory.Exists(debugFolder))
                    Directory.CreateDirectory(debugFolder);
                    
                originalImage.Save(Path.Combine(debugFolder, "original.png"));
                
                List<Bitmap> processedImages = new List<Bitmap>();
                
                try
                {
                    Bitmap scaled1 = ScaleImage(originalImage, 4.0f);
                    Bitmap grayscale1 = ConvertToGrayscale(scaled1);
                    scaled1.Dispose();
                    Bitmap colorIsolated = IsolatePinkMagentaText(grayscale1);
                    grayscale1.Dispose();
                    Bitmap thresholded1 = ApplyAdaptiveThreshold(colorIsolated);
                    colorIsolated.Dispose();
                    Bitmap cleaned1 = ApplyMorphologicalOperations(thresholded1);
                    thresholded1.Dispose();
                    processedImages.Add(cleaned1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Approach 1 failed: {ex.Message}");
                }
                
                if (processedImages.Count > 0)
                {
                    return processedImages[0];
                }
                
                return new Bitmap(originalImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PreprocessCaptchaImage: {ex.Message}");
                return new Bitmap(originalImage);
            }
        }

        public static Bitmap ScaleImage(Bitmap image, float scale)
        {
            int newWidth = (int)(image.Width * scale);
            int newHeight = (int)(image.Height * scale);
            Bitmap result = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return result;
        }

        public static Bitmap ApplyThreshold(Bitmap image, int threshold)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    result.SetPixel(x, y, gray > threshold ? Color.White : Color.Black);
                }
            }
            return result;
        }

        public static Bitmap ApplyAdaptiveThreshold(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            long totalBrightness = 0;
            int pixelCount = image.Width * image.Height;
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    totalBrightness += (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                }
            }
            int averageBrightness = (int)(totalBrightness / pixelCount);
            int threshold = Math.Max(100, averageBrightness - 20);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    result.SetPixel(x, y, gray > threshold ? Color.White : Color.Black);
                }
            }
            return result;
        }
        
        public static Bitmap IsolatePinkMagentaText(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    bool isPinkMagenta = (pixel.R > 180 && pixel.G < 120 && pixel.B > 120) || (pixel.R > 150 && pixel.G < 100 && pixel.B > 180) || (pixel.R > 120 && pixel.G < 80 && pixel.B > 150);
                    result.SetPixel(x, y, isPinkMagenta ? Color.Black : Color.White);
                }
            }
            return result;
        }
        
        public static Bitmap ConvertToGrayscale(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    result.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }
            return result;
        }
        
        public static Bitmap InvertImage(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    result.SetPixel(x, y, Color.FromArgb(255 - pixel.R, 255 - pixel.G, 255 - pixel.B));
                }
            }
            return result;
        }
        
        public static void SaveOptimizedImage(Bitmap image, string filePath)
        {
            try
            {
                string? directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                using (EncoderParameters encoderParams = new EncoderParameters(1))
                {
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 80L);
                    ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                    if (jpegCodec != null)
                    {
                        image.Save(filePath, jpegCodec, encoderParams);
                    }
                    else
                    {
                        image.Save(filePath, ImageFormat.Png);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi khi luu anh toi uu: {ex.Message}");
                try { image.Save(filePath); }
                catch { Console.WriteLine("Khong the luu anh"); }
            }
        }
        
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.MimeType == mimeType)!;
        }
        
        public static Bitmap ApplyMorphologicalOperations(Bitmap image)
        {
            Bitmap eroded = ApplyErosion(image);
            Bitmap dilated = ApplyDilation(eroded);
            eroded.Dispose();
            return dilated;
        }
        
        private static Bitmap ApplyErosion(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    bool allBlack = true;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (image.GetPixel(x + i, y + j).R > 128) { allBlack = false; break; }
                        }
                        if (!allBlack) break;
                    }
                    result.SetPixel(x, y, allBlack ? Color.Black : Color.White);
                }
            }
            return result;
        }
        
        private static Bitmap ApplyDilation(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            for (int x = 1; x < image.Width - 1; x++)
            {
                for (int y = 1; y < image.Height - 1; y++)
                {
                    bool anyBlack = false;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (image.GetPixel(x + i, y + j).R < 128) { anyBlack = true; break; }
                        }
                        if (anyBlack) break;
                    }
                    result.SetPixel(x, y, anyBlack ? Color.Black : Color.White);
                }
            }
            return result;
        }
    }
}