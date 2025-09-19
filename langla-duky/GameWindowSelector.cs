#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using langla_duky.Models;

namespace langla_duky
{
    public partial class GameWindowSelector : Form
    {
        private readonly ListBox _windowsList;
        private readonly Button _btnRefresh;
        private readonly Button _btnSelect;
        private readonly Button _btnCancel;
        private readonly PictureBox _previewBox;
        private readonly Label _lblPreview;
        private readonly Label _lblInstructions;
        private List<GameWindow> _availableWindows;

        public GameWindow? SelectedWindow { get; private set; }

        public GameWindowSelector()
        {
            _windowsList = new ListBox();
            _btnRefresh = new Button();
            _btnSelect = new Button();
            _btnCancel = new Button();
            _previewBox = new PictureBox();
            _lblPreview = new Label();
            _lblInstructions = new Label();
            _availableWindows = new List<GameWindow>();

            InitializeComponent();
            LoadAvailableWindows();
        }

        private void InitializeComponent()
        {
            Text = "Chọn cửa sổ game";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Instructions
            _lblInstructions.Text = "Chọn cửa sổ game từ danh sách bên dưới. Hình ảnh preview sẽ hiển thị để bạn xác nhận đúng cửa sổ.";
            _lblInstructions.Location = new Point(20, 20);
            _lblInstructions.Size = new Size(760, 40);
            _lblInstructions.Font = new Font("Arial", 10);
            Controls.Add(_lblInstructions);

            // Windows list
            _windowsList.Location = new Point(20, 70);
            _windowsList.Size = new Size(350, 400);
            _windowsList.Font = new Font("Arial", 10);
            _windowsList.SelectedIndexChanged += WindowsList_SelectedIndexChanged;
            Controls.Add(_windowsList);

            // Preview label
            _lblPreview.Text = "Preview cửa sổ game:";
            _lblPreview.Location = new Point(390, 70);
            _lblPreview.Size = new Size(200, 20);
            _lblPreview.Font = new Font("Arial", 10);
            Controls.Add(_lblPreview);

            // Preview box
            _previewBox.Location = new Point(390, 100);
            _previewBox.Size = new Size(380, 300);
            _previewBox.BorderStyle = BorderStyle.FixedSingle;
            _previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            Controls.Add(_previewBox);

            // Refresh button
            _btnRefresh.Text = "Làm mới danh sách";
            _btnRefresh.Location = new Point(20, 490);
            _btnRefresh.Size = new Size(150, 30);
            _btnRefresh.BackColor = Color.LightBlue;
            _btnRefresh.Click += BtnRefresh_Click;
            Controls.Add(_btnRefresh);

            // Select button
            _btnSelect.Text = "Chọn cửa sổ này";
            _btnSelect.Location = new Point(520, 490);
            _btnSelect.Size = new Size(120, 30);
            _btnSelect.BackColor = Color.LightGreen;
            _btnSelect.Enabled = false;
            _btnSelect.Click += BtnSelect_Click;
            Controls.Add(_btnSelect);

            // Cancel button
            _btnCancel.Text = "Hủy";
            _btnCancel.Location = new Point(650, 490);
            _btnCancel.Size = new Size(120, 30);
            _btnCancel.BackColor = Color.LightCoral;
            _btnCancel.Click += BtnCancel_Click;
            Controls.Add(_btnCancel);
        }

        private void LoadAvailableWindows()
        {
            try
            {
                _availableWindows = new List<GameWindow>();
                _windowsList.Items.Clear();

                // Chỉ tìm các ứng dụng Java đang chạy
                var javaWindows = FindJavaProcessWindows();
                
                // Hiển thị cửa sổ Java
                if (javaWindows != null && javaWindows.Count > 0)
                {
                    foreach (var window in javaWindows)
                    {
                        if (window != null)
                        {
                            if (!window.IsValid())
                            {
                                window.UpdateWindowInfo();
                            }
                            
                            _availableWindows.Add(window);
                            _windowsList.Items.Add($"[JAVA] {window.WindowTitle} ({window.Bounds.Width}x{window.Bounds.Height})");
                            Console.WriteLine($"Added Java window: {window.WindowTitle} (Handle: {window.Handle})");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Không tìm thấy ứng dụng Java nào đang chạy");
                    _windowsList.Items.Add("--- Không tìm thấy ứng dụng Java nào đang chạy ---");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi load windows: {ex.Message}");
                _windowsList.Items.Add("--- Lỗi khi tải danh sách cửa sổ ---");
            }

            // Thêm tùy chọn làm mới danh sách
            _windowsList.Items.Add("--- Làm mới danh sách ---");
            
            // Tự động chọn cửa sổ Java đầu tiên nếu có
            if (_availableWindows.Count > 0 && _windowsList.Items.Count > 0)
            {
                _windowsList.SelectedIndex = 0;
            }
            
            Console.WriteLine($"Loaded {_availableWindows.Count} Java windows");
        }
        
        private List<GameWindow> FindJavaProcessWindows()
        {
            var javaWindows = new List<GameWindow>();
            var processedHandles = new HashSet<IntPtr>(); // Để tránh trùng lặp cửa sổ
            
            try
            {
                Console.WriteLine("Đang tìm các ứng dụng Java đang chạy...");
                
                // Phương pháp 1: Tìm bằng Process.GetProcesses()
                var processes = System.Diagnostics.Process.GetProcesses();
                if (processes != null)
                {
                    foreach (System.Diagnostics.Process p in processes)
                    {
                        try
                        {
                            // Chỉ lấy process có tên java hoặc javaw
                            if (p != null && p.ProcessName != null && p.ProcessName.ToLower().Contains("java"))
                            {
                                string title = p.MainWindowTitle ?? string.Empty;
                                IntPtr hWnd = p.MainWindowHandle;

                                if (!string.IsNullOrWhiteSpace(title) && hWnd != IntPtr.Zero && !processedHandles.Contains(hWnd))
                                {
                                    processedHandles.Add(hWnd);
                                    Console.WriteLine($"Java App: {p.ProcessName} | PID: {p.Id} | Title: {title} | Handle: {hWnd}");
                                    
                                    // Tạo GameWindow trực tiếp từ handle
                                    var window = CreateGameWindowFromHandle(hWnd, title);
                                    if (window != null)
                                    {
                                        javaWindows.Add(window);
                                        Console.WriteLine($"Found Java window: {title} (Handle: {hWnd})");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Lỗi khi xử lý process: {ex.Message}");
                        }
                        finally
                        {
                            p?.Dispose();
                        }
                    }
                }
                
                // Phương pháp 2: Tìm bằng EnumWindows để bắt các cửa sổ Java khác
                EnumWindows((hwnd, lParam) => 
                {
                    try
                    {
                        if (IsWindowVisible(hwnd) && !processedHandles.Contains(hwnd))
                        {
                            StringBuilder titleBuilder = new StringBuilder(256);
                            if (GetWindowText(hwnd, titleBuilder, 256) > 0)
                            {
                                string title = titleBuilder.ToString();
                                if (!string.IsNullOrEmpty(title) && (title.Contains("Duke") || title.Contains("Java")))
                                {
                                    GetWindowThreadProcessId(hwnd, out uint processId);
                                    
                                    try
                                    {
                                        using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                                        if (process != null && process.ProcessName != null && process.ProcessName.ToLower().Contains("java"))
                                        {
                                            processedHandles.Add(hwnd);
                                            var window = CreateGameWindowFromHandle(hwnd, title);
                                            if (window != null)
                                            {
                                                javaWindows.Add(window);
                                                Console.WriteLine($"Found Java window (EnumWindows): {title} (Handle: {hwnd})");
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Process không tồn tại hoặc không truy cập được
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi trong EnumWindows: {ex.Message}");
                    }
                    return true;
                }, IntPtr.Zero);
                
                // Đảm bảo không có cửa sổ trùng lặp
                if (javaWindows != null && javaWindows.Count > 0)
                {
                    javaWindows = javaWindows.Where(w => w != null).GroupBy(w => w.Handle).Select(g => g.First()).ToList();
                }
                
                Console.WriteLine($"Tìm thấy {javaWindows?.Count ?? 0} cửa sổ Java độc lập");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tìm ứng dụng Java: {ex.Message}");
            }
            
            return javaWindows ?? new List<GameWindow>();
        }
        
        private GameWindow? CreateGameWindowFromHandle(IntPtr hWnd, string title)
        {
            try
            {
                if (hWnd == IntPtr.Zero || string.IsNullOrEmpty(title))
                    return null;
                
                RECT rect;
                if (!GetWindowRect(hWnd, out rect))
                    return null;
                
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                
                if (width <= 0 || height <= 0)
                    return null;
                
                var window = new GameWindow(title);
                
                // Gán handle trực tiếp
                window.SetHandle(hWnd);
                
                // Cập nhật thông tin cửa sổ
                window.UpdateWindowInfo();
                
                return window;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating window from handle: {ex.Message}");
                return null;
            }
        }
        
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void WindowsList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                string? selectedItem = _windowsList.SelectedItem?.ToString();
                int index = _windowsList.SelectedIndex;
                
                // Xử lý các mục đặc biệt
                if (selectedItem == "--- Làm mới danh sách ---")
                {
                    // Làm mới danh sách
                    LoadAvailableWindows();
                    
                    // Xóa preview
                    if (_previewBox.Image != null)
                    {
                        _previewBox.Image.Dispose();
                        _previewBox.Image = null;
                    }
                    _btnSelect.Enabled = false;
                    return;
                }
                else if (selectedItem == "--- Không tìm thấy ứng dụng Java nào đang chạy ---")
                {
                    // Không có cửa sổ Java, xóa preview
                    if (_previewBox.Image != null)
                    {
                        _previewBox.Image.Dispose();
                        _previewBox.Image = null;
                    }
                    _btnSelect.Enabled = false;
                    return;
                }
                
                // Xử lý mục cửa sổ Java
                if (index >= 0 && index < _availableWindows.Count)
                {
                    var selectedWindow = _availableWindows[index];
                    Console.WriteLine($"Selected Java window: {selectedWindow.WindowTitle} (Handle: {selectedWindow.Handle})");
                    
                    // Đảm bảo handle cửa sổ được cập nhật
                    if (!selectedWindow.IsValid())
                    {
                        Console.WriteLine($"Window not valid, updating info: {selectedWindow.WindowTitle}");
                        selectedWindow.UpdateWindowInfo();
                    }
                    
                    // Cập nhật preview
                    UpdatePreview(selectedWindow);
                    _btnSelect.Enabled = true;
                }
                else
                {
                    // Xóa preview nếu không có cửa sổ nào được chọn
                    if (_previewBox.Image != null)
                    {
                        _previewBox.Image.Dispose();
                        _previewBox.Image = null;
                    }
                    _btnSelect.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WindowsList_SelectedIndexChanged: {ex.Message}");
                if (_previewBox.Image != null)
                {
                    _previewBox.Image.Dispose();
                    _previewBox.Image = null;
                }
                _btnSelect.Enabled = false;
            }
        }

        private void UpdatePreview(GameWindow window)
        {
            try
            {
                // Xóa hình ảnh cũ
                if (_previewBox.Image != null)
                {
                    _previewBox.Image.Dispose();
                    _previewBox.Image = null;
                }

                // Đảm bảo handle cửa sổ được cập nhật và hợp lệ
                if (window.Handle == IntPtr.Zero || !IsWindow(window.Handle))
                {
                    Console.WriteLine($"Handle không hợp lệ, thử tìm lại cửa sổ: {window.WindowTitle}");
                    window.FindGameWindow();
                }
                
                // Cập nhật thông tin cửa sổ
                window.UpdateWindowInfo();

                // Capture cửa sổ để hiển thị preview
                if (window.Handle != IntPtr.Zero && IsWindow(window.Handle))
                {
                    Console.WriteLine($"Capturing preview for window: {window.WindowTitle} (Handle: {window.Handle})");
                    
                    // Đảm bảo cửa sổ có kích thước hợp lệ
                    if (window.Bounds.Width <= 0 || window.Bounds.Height <= 0)
                    {
                        Console.WriteLine($"Kích thước cửa sổ không hợp lệ: {window.Bounds}");
                        return;
                    }
                    
                    // Hiển thị thông tin chi tiết trước khi capture
                    _lblPreview.Text = $"Đang tạo preview cho: {window.WindowTitle} (Handle: {window.Handle})";
                    Application.DoEvents(); // Cho phép UI cập nhật
                    
                    // Capture toàn bộ cửa sổ - sử dụng phương thức capture mới
                    using var capture = CaptureWindowDirect(window.Handle);
                    
                    if (capture != null)
                    {
                        // Lưu hình ảnh để debug
                        string debugFolder = "window_previews";
                        Directory.CreateDirectory(debugFolder);
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                        string filename = Path.Combine(debugFolder, $"preview_{timestamp}_handle_{window.Handle}.png");
                        capture.Save(filename);
                        
                        // Hiển thị hình ảnh
                        _previewBox.Image = new Bitmap(capture);
                        
                        // Hiển thị thông tin chi tiết
                        _lblPreview.Text = $"Preview: {window.WindowTitle} (Handle: {window.Handle})";
                        Console.WriteLine($"Preview updated for: {window.WindowTitle}");
                    }
                    else
                    {
                        _lblPreview.Text = $"Không thể capture cửa sổ: {window.WindowTitle}";
                        Console.WriteLine($"Failed to capture window: {window.WindowTitle}");
                    }
                }
                else
                {
                    _lblPreview.Text = $"Không thể tạo preview: Handle không hợp lệ";
                    Console.WriteLine($"Invalid handle for window: {window.WindowTitle}");
                }
            }
            catch (Exception ex)
            {
                if (_previewBox.Image != null)
                {
                    _previewBox.Image.Dispose();
                    _previewBox.Image = null;
                }
                _lblPreview.Text = $"Lỗi khi tạo preview: {ex.Message}";
                Console.WriteLine($"Error creating preview: {ex.Message}");
            }
        }
        
        // Phương thức capture cửa sổ trực tiếp từ handle
        private Bitmap? CaptureWindowDirect(IntPtr hWnd)
        {
            try
            {
                // Kiểm tra handle
                if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                {
                    Console.WriteLine("Invalid window handle");
                    return null;
                }
                
                // Lấy kích thước cửa sổ
                RECT rect;
                if (!GetWindowRect(hWnd, out rect))
                {
                    Console.WriteLine("Failed to get window rect");
                    return null;
                }
                
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                
                if (width <= 0 || height <= 0 || width > 3000 || height > 3000)
                {
                    Console.WriteLine($"Invalid window size: {width}x{height}");
                    return null;
                }
                
                // Capture cửa sổ
                Bitmap bitmap = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // Sử dụng PrintWindow để capture
                    IntPtr hdcBitmap = g.GetHdc();
                    bool success = PrintWindow(hWnd, hdcBitmap, 0);
                    g.ReleaseHdc(hdcBitmap);
                    
                    if (!success)
                    {
                        Console.WriteLine("PrintWindow failed");
                        
                        // Thử phương pháp thay thế
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
                    }
                }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CaptureWindowDirect: {ex.Message}");
                return null;
            }
        }
        
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            Console.WriteLine("Làm mới danh sách cửa sổ Java...");
            LoadAvailableWindows();
            
            // Hiển thị thông báo
            _lblPreview.Text = "Đã làm mới danh sách cửa sổ Java";
        }

        private void BtnSelect_Click(object? sender, EventArgs e)
        {
            try
            {
                int index = _windowsList.SelectedIndex;
                if (index >= 0 && index < _availableWindows.Count && _availableWindows[index] != null)
                {
                    SelectedWindow = _availableWindows[index];
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một cửa sổ game hợp lệ từ danh sách.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chọn cửa sổ: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_previewBox.Image != null)
            {
                _previewBox.Image.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _windowsList.Dispose();
                _btnRefresh.Dispose();
                _btnSelect.Dispose();
                _btnCancel.Dispose();
                _previewBox.Dispose();
                _lblPreview.Dispose();
                _lblInstructions.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}