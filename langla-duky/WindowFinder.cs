using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using langla_duky.Models;

namespace langla_duky
{
    public class WindowFinder
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out GameWindow.RECT lpRect);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static string FindAllWindows()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("=== DANH SÁCH TẤT CẢ CỬA SỔ ĐANG MỞ ===");
            result.AppendLine();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(hWnd, windowText, 256);
                    
                    if (windowText.Length > 0)
                    {
                        GetWindowThreadProcessId(hWnd, out uint processId);
                        result.AppendLine($"Title: '{windowText}' | Handle: {hWnd} | PID: {processId}");
                    }
                }
                return true;
            }, IntPtr.Zero);

            result.AppendLine();
            result.AppendLine("=== KẾT THÚC DANH SÁCH ===");
            return result.ToString();
        }

        public static string FindGameWindows()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("=== TÌM CỬA SỔ GAME ===");
            result.AppendLine();

            // Thử tìm theo process Java trước
            result.AppendLine("--- Tìm theo Process 'java' ---");
            var javaWindows = FindWindowsByProcess("java");
            foreach (var window in javaWindows)
            {
                result.AppendLine($"✓ Java Process: '{window.WindowTitle}' ({window.Bounds.Width}x{window.Bounds.Height})");
            }

            result.AppendLine();
            result.AppendLine("--- Tìm theo Title ---");
            string[] possibleTitles = {
                "Duke Client - By iamDuke",
                "Làng Lá Duke", 
                "Duke Client",
                "Duke",
                "Làng Lá",
                "iamDuke"
            };

            foreach (string title in possibleTitles)
            {
                IntPtr hWnd = FindWindow(null, title);
                if (hWnd != IntPtr.Zero)
                {
                    GameWindow.RECT rect;
                    GetWindowRect(hWnd, out rect);
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    
                    result.AppendLine($"✓ Tìm thấy: '{title}' | Handle: {hWnd} | Kích thước: {width}x{height}");
                }
                else
                {
                    result.AppendLine($"✗ Không tìm thấy: '{title}'");
                }
            }

            result.AppendLine();
            result.AppendLine("=== KẾT THÚC TÌM KIẾM ===");
            return result.ToString();
        }

        public static List<GameWindow> FindWindowsByProcess(string processName)
        {
            var windows = new List<GameWindow>();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    GetWindowThreadProcessId(hWnd, out uint processId);
                    IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
                    
                    if (hProcess != IntPtr.Zero)
                    {
                        StringBuilder processNameBuffer = new StringBuilder(256);
                        if (GetModuleBaseName(hProcess, IntPtr.Zero, processNameBuffer, 256) > 0)
                        {
                            string currentProcessName = processNameBuffer.ToString();
                            if (currentProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase))
                            {
                                StringBuilder windowText = new StringBuilder(256);
                                GetWindowText(hWnd, windowText, 256);
                                
                                if (windowText.Length > 0)
                                {
                                    var window = new GameWindow(windowText.ToString());
                                    // Không thể set Handle trực tiếp, dùng phương thức khác
                                    if (window.FindGameWindow())
                                    {
                                        windows.Add(window);
                                    }
                                    else
                                    {
                                        // Tạo một phương thức mới trong GameWindow để cập nhật handle
                                        var gameWindow = CreateGameWindowFromHandle(hWnd, windowText.ToString());
                                        if (gameWindow != null)
                                        {
                                            windows.Add(gameWindow);
                                        }
                                    }
                                }
                            }
                        }
                        CloseHandle(hProcess);
                    }
                }
                return true;
            }, IntPtr.Zero);
            
            return windows;
        }
        
        // Phương thức tạo GameWindow từ handle
        private static GameWindow? CreateGameWindowFromHandle(IntPtr hWnd, string title)
        {
            var window = new GameWindow(title);
            
            // Cập nhật thông tin cửa sổ
            GameWindow.RECT rect;
            if (GetWindowRect(hWnd, out rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                
                // Gọi FindGameWindow để cập nhật handle và bounds
                if (window.FindGameWindow())
                {
                    return window;
                }
            }
            
            return null;
        }

        public static List<GameWindow> GetAllVisibleWindows()
        {
            var windows = new List<GameWindow>();
            
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder windowText = new StringBuilder(256);
                    GetWindowText(hWnd, windowText, 256);
                    
                    if (windowText.Length > 0)
                    {
                        // Lấy kích thước cửa sổ
                        GameWindow.RECT rect;
                        GetWindowRect(hWnd, out rect);
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        
                        // Chỉ thêm các cửa sổ có kích thước hợp lý (loại bỏ các cửa sổ quá nhỏ)
                        if (width > 100 && height > 100)
                        {
                            var window = new GameWindow(windowText.ToString());
                            if (window.FindGameWindow())
                            {
                                windows.Add(window);
                            }
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);
            
            // Sắp xếp theo kích thước giảm dần (cửa sổ lớn nhất lên đầu)
            windows.Sort((a, b) => (b.Bounds.Width * b.Bounds.Height).CompareTo(a.Bounds.Width * a.Bounds.Height));
            
            return windows;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;
        
        /// <summary>
        /// Lấy danh sách các cửa sổ game
        /// </summary>
        public static List<GameWindow> GetGameWindows()
        {
            var gameWindows = new List<GameWindow>();
            
            // Tìm theo process Java
            var javaWindows = FindWindowsByProcess("java");
            if (javaWindows.Count > 0)
            {
                gameWindows.AddRange(javaWindows);
            }
            
            // Tìm theo title
            string[] possibleTitles = {
                "Duke Client - By iamDuke",
                "Làng Lá Duke", 
                "Duke Client",
                "Duke",
                "Làng Lá",
                "iamDuke"
            };
            
            foreach (string title in possibleTitles)
            {
                IntPtr hWnd = FindWindow(null, title);
                if (hWnd != IntPtr.Zero)
                {
                    var window = new GameWindow(title);
                    if (window.FindGameWindow())
                    {
                        // Kiểm tra xem cửa sổ này đã có trong danh sách chưa
                        bool isDuplicate = false;
                        foreach (var existingWindow in gameWindows)
                        {
                            if (existingWindow.WindowTitle == window.WindowTitle)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                        
                        if (!isDuplicate)
                        {
                            gameWindows.Add(window);
                        }
                    }
                }
            }
            
            // Nếu không tìm thấy cửa sổ game, lấy tất cả cửa sổ có kích thước hợp lý
            if (gameWindows.Count == 0)
            {
                var allWindows = GetAllVisibleWindows();
                
                // Lọc các cửa sổ có kích thước phù hợp với game (ít nhất 800x600)
                var suitableWindows = allWindows.Where(w => w.Bounds.Width >= 800 && w.Bounds.Height >= 600).ToList();
                
                if (suitableWindows.Count > 0)
                {
                    gameWindows.AddRange(suitableWindows);
                }
            }
            
            return gameWindows;
        }
    }
}