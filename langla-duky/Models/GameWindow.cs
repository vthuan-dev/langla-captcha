using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace langla_duky.Models
{
    public class GameWindow
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public string WindowTitle { get; private set; }
        public string Title => WindowTitle;
        public IntPtr Handle { get; private set; }
        public Rectangle Bounds { get; private set; }

        public GameWindow(string windowTitle)
        {
            WindowTitle = windowTitle;
            Handle = IntPtr.Zero;
            Bounds = Rectangle.Empty;
            FindGameWindow();
        }

        public bool IsValid()
        {
            return Handle != IntPtr.Zero;
        }

        public bool FindGameWindow()
        {
            Handle = FindWindow(null, WindowTitle);
            if (Handle != IntPtr.Zero)
            {
                UpdateWindowInfo();
                return true;
            }
            return false;
        }

        public void SetHandle(IntPtr handle)
        {
            Handle = handle;
            UpdateWindowInfo();
        }

        public void UpdateBounds()
        {
            if (Handle != IntPtr.Zero)
            {
                RECT rect;
                if (GetWindowRect(Handle, out rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    Bounds = new Rectangle(rect.Left, rect.Top, width, height);
                }
            }
        }

        public void UpdateWindowInfo()
        {
            if (Handle != IntPtr.Zero)
            {
                // Update title
                StringBuilder titleBuilder = new StringBuilder(256);
                if (GetWindowText(Handle, titleBuilder, 256) > 0)
                {
                    WindowTitle = titleBuilder.ToString();
                }

                // Update bounds
                UpdateBounds();
            }
        }

        public bool FindGameWindowWithMultipleInstances()
        {
            // This method searches for multiple instances of the game window
            // For now, it just calls the regular FindGameWindow method
            return FindGameWindow();
        }

        public override string ToString()
        {
            return $"{WindowTitle} ({Handle})";
        }
    }
}