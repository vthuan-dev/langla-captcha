using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;

namespace langla_duky.Models
{
    public class InputAutomation
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        // Windows Messages
        private const uint WM_CHAR = 0x0102;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;

        // Mouse Events
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_MOVE = 0x0001;

        // Virtual Key Codes
        private const int VK_RETURN = 0x0D;
        private const int VK_TAB = 0x09;

        public static bool SendTextToWindow(IntPtr windowHandle, string text)
        {
            if (windowHandle == IntPtr.Zero || string.IsNullOrEmpty(text))
                return false;

            try
            {
                // Bring window to front
                SetForegroundWindow(windowHandle);
                Thread.Sleep(100); // Small delay

                // Send each character
                foreach (char c in text)
                {
                    SendMessage(windowHandle, WM_CHAR, new IntPtr(c), IntPtr.Zero);
                    Thread.Sleep(50); // Small delay between characters
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi text: {ex.Message}");
                return false;
            }
        }

        public static bool SendKeyPress(IntPtr windowHandle, int virtualKeyCode)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            try
            {
                SetForegroundWindow(windowHandle);
                Thread.Sleep(50);

                // Send key down
                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr(virtualKeyCode), IntPtr.Zero);
                Thread.Sleep(50);

                // Send key up
                PostMessage(windowHandle, WM_KEYUP, new IntPtr(virtualKeyCode), IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi key: {ex.Message}");
                return false;
            }
        }

        public static bool ClickAtPosition(int x, int y)
        {
            try
            {
                SetCursorPos(x, y);
                Thread.Sleep(50);

                mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, UIntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi click: {ex.Message}");
                return false;
            }
        }

        public static bool ClickInWindow(IntPtr windowHandle, Point relativePosition)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            try
            {
                SetForegroundWindow(windowHandle);
                Thread.Sleep(100);

                // Convert client (relative) position to screen coordinates
                Point clientOrigin = new Point(0, 0);
                ClientToScreen(windowHandle, ref clientOrigin);
                Point screenPos = new Point(clientOrigin.X + relativePosition.X, clientOrigin.Y + relativePosition.Y);
                
                return ClickAtPosition(screenPos.X, screenPos.Y);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi click trong window: {ex.Message}");
                return false;
            }
        }

        public static bool SendEnterKey(IntPtr windowHandle)
        {
            return SendKeyPress(windowHandle, VK_RETURN);
        }

        public static bool SendTabKey(IntPtr windowHandle)
        {
            return SendKeyPress(windowHandle, VK_TAB);
        }
    }
}
