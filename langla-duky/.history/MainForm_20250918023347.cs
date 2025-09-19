using System;
using System.Drawing;
using System.Windows.Forms;
using langla_duky.Models;
using langla_duky.Services;
using System.Drawing.Imaging;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

public static class ControlExtensions
{
    public static Task InvokeAsync(this Control control, Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        if (control.InvokeRequired)
        {
            control.BeginInvoke(new Action(() =>
            {
                try { action(); tcs.TrySetResult(true); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            }));
        }
        else
        {
            try { action(); tcs.TrySetResult(true); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        }
        return tcs.Task;
    }
}

namespace langla_duky
{
    public partial class MainForm : Form
    {
        private CaptchaAutomationService _automationService = null!;
        private OCRSpaceService _ocrSpaceService = null!;
        private Config _config = null!;

        private GroupBox grpGameWindow = null!;
        private GroupBox grpCaptchaInfo = null!;
        private GroupBox grpMainControls = null!;
        private GroupBox grpAdvancedTools = null!;
        private GroupBox grpMonitoring = null!;
        private GroupBox grpPreview = null!;
        private GroupBox grpLogOutput = null!;

        private Button _btnStart = null!;
        private Button _btnStop = null!;
        private Button _btnTest = null!;
        private Button _btnDebug = null!;
        private Button _btnSelectWindow = null!;
        private Button _btnStartMonitoring = null!;
        private Button _btnStopMonitoring = null!;
        private Button _btnCaptureDebug = null!;
        private Button _btnQuickOCR = null!;
        private Button _btnDukeCaptcha = null!;

        private Label _lblStatus = null!;
        private Label _lblCaptcha = null!;
        private Label _lblStats = null!;
        private Label _lblSelectedWindow = null!;
        private Label _lblMonitoringStatus = null!;
        private Label _lblPerformanceStats = null!;
        private Label _lblImageInfo = null!;

        private TextBox _txtLog = null!;
        private PictureBox _picCaptcha = null!;
        private PictureBox _picGamePreview = null!;
        private CheckBox _chkUseOCRAPI = null!;

        private System.Windows.Forms.Timer _statusTimer = null!;
        private System.Windows.Forms.Timer _previewTimer = null!;
        private GameWindow? _selectedGameWindow;
        
        private CancellationTokenSource? _monitoringCts;
        private Task? _monitoringTask;
        private bool _isMonitoring = false;

        private static readonly Color PrimaryBlue = Color.FromArgb(41, 128, 185);
        private static readonly Color SecondaryBlue = Color.FromArgb(52, 152, 219);
        private static readonly Color SuccessGreen = Color.FromArgb(39, 174, 96);
        private static readonly Color WarningOrange = Color.FromArgb(230, 126, 34);
        private static readonly Color DangerRed = Color.FromArgb(231, 76, 60);
        private static readonly Color LightGray = Color.FromArgb(236, 240, 241);
        private static readonly Color DarkGray = Color.FromArgb(52, 73, 94);

        public struct BoxSize
        {
            public int Width { get; }
            public int Height { get; }
            public BoxSize(int width, int height) { Width = width; Height = height; }
        }

        public MainForm()
        {
            InitializeComponent();
            _config = Config.LoadFromFile();
            InitializeAutomationService();
        }

        private Bitmap FitTo(BoxSize target, Bitmap src)
        {
            var result = new Bitmap(target.Width, target.Height);
            result.SetResolution(src.HorizontalResolution, src.VerticalResolution);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                float scale = Math.Min((float)target.Width / src.Width, (float)target.Height / src.Height);
                int newWidth = (int)(src.Width * scale);
                int newHeight = (int)(src.Height * scale);
                int x = (target.Width - newWidth) / 2;
                int y = (target.Height - newHeight) / 2;
                g.DrawImage(src, new Rectangle(x, y, newWidth, newHeight));
            }
            return result;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Láng Lá Duke - Captcha Automation Tool";
            this.Size = new Size(1400, 850);
            this.MinimumSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = LightGray;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            InitializeGroups();
            InitializeGameWindowGroup();
            InitializeCaptchaInfoGroup();
            InitializeMainControlsGroup();
            InitializeAdvancedToolsGroup();
            InitializeMonitoringGroup();
            InitializePreviewGroup();
            InitializeLogOutputGroup();
            InitializeTimers();
            this.ResumeLayout(false);
        }

        private void InitializeGroups()
        {
            grpGameWindow = new GroupBox { Text = "🎮 Game Window", Location = new Point(20, 20), Size = new Size(350, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpGameWindow);
            grpCaptchaInfo = new GroupBox { Text = "🔍 Captcha Information", Location = new Point(390, 20), Size = new Size(350, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpCaptchaInfo);
            grpMainControls = new GroupBox { Text = "⚡ Main Controls", Location = new Point(20, 160), Size = new Size(350, 100), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpMainControls);
            grpAdvancedTools = new GroupBox { Text = "🛠️ Advanced Tools", Location = new Point(390, 160), Size = new Size(350, 100), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpAdvancedTools);
            grpMonitoring = new GroupBox { Text = "📊 Monitoring & Status", Location = new Point(20, 280), Size = new Size(720, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpMonitoring);
            grpPreview = new GroupBox { Text = "👁️ Preview & Images", Location = new Point(760, 20), Size = new Size(600, 380), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpPreview);
            grpLogOutput = new GroupBox { Text = "📝 Log Output", Location = new Point(20, 420), Size = new Size(1340, 380), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpLogOutput);
        }

        private void InitializeGameWindowGroup()
        {
            _lblSelectedWindow = new Label { Text = "Cửa sổ game: Chưa chọn", Location = new Point(15, 25), Size = new Size(320, 20), Font = new Font("Segoe UI", 9F), ForeColor = DangerRed };
            grpGameWindow.Controls.Add(_lblSelectedWindow);
            _btnSelectWindow = CreateModernButton("Chọn cửa sổ", new Point(15, 55), new Size(150, 35), WarningOrange);
            _btnSelectWindow.Click += BtnSelectWindow_Click;
            grpGameWindow.Controls.Add(_btnSelectWindow);
            var btnAutoDetect = CreateModernButton("Tự động tìm", new Point(180, 55), new Size(150, 35), SecondaryBlue);
            btnAutoDetect.Click += async (s, e) => await AutoDetectGameWindow();
            grpGameWindow.Controls.Add(btnAutoDetect);
        }

        private void InitializeCaptchaInfoGroup()
        {
            _lblCaptcha = new Label { Text = "Captcha: Chưa có", Location = new Point(15, 25), Size = new Size(320, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            grpCaptchaInfo.Controls.Add(_lblCaptcha);
            _lblStats = new Label { Text = "Thành công: 0 | Thất bại: 0", Location = new Point(15, 50), Size = new Size(320, 20), Font = new Font("Segoe UI", 9F) };
            grpCaptchaInfo.Controls.Add(_lblStats);
            _chkUseOCRAPI = new CheckBox { Text = "🌐 Use OCR.space API", Location = new Point(15, 75), Size = new Size(200, 25), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = SecondaryBlue, Checked = _config?.OCRSettings?.UseOCRAPI ?? true };
            _chkUseOCRAPI.CheckedChanged += (s, e) =>
            {
                if (_config?.OCRSettings != null)
                {
                    _config.OCRSettings.UseOCRAPI = _chkUseOCRAPI.Checked;
                    LogMessage($"OCR Method: {(_config.OCRSettings.UseOCRAPI ? "OCR.space API" : "Tesseract Local")}");
                }
            };
            grpCaptchaInfo.Controls.Add(_chkUseOCRAPI);
        }

        private void InitializeMainControlsGroup()
        {
            _btnStart = CreateModernButton("▶️ Bắt đầu", new Point(15, 25), new Size(100, 40), SuccessGreen);
            _btnStart.Click += BtnStart_Click;
            grpMainControls.Controls.Add(_btnStart);
            _btnStop = CreateModernButton("⏹️ Dừng", new Point(130, 25), new Size(100, 40), DangerRed);
            _btnStop.Click += BtnStop_Click;
            _btnStop.Enabled = false;
            grpMainControls.Controls.Add(_btnStop);
            _btnTest = CreateModernButton("🧪 Test OCR", new Point(245, 25), new Size(90, 40), SecondaryBlue);
            _btnTest.Click += BtnTest_Click;
            grpMainControls.Controls.Add(_btnTest);
        }

        private void InitializeAdvancedToolsGroup()
        {
            _btnDebug = CreateModernButton("🐛 Debug", new Point(15, 25), new Size(80, 40), WarningOrange);
            _btnDebug.Click += BtnDebug_Click;
            grpAdvancedTools.Controls.Add(_btnDebug);
            _btnCaptureDebug = CreateModernButton("📷 Capture", new Point(105, 25), new Size(80, 40), WarningOrange);
            _btnCaptureDebug.Click += BtnCaptureDebug_Click;
            grpAdvancedTools.Controls.Add(_btnCaptureDebug);
            _btnQuickOCR = CreateModernButton("⚡ Quick OCR", new Point(195, 25), new Size(80, 40), SecondaryBlue);
            _btnQuickOCR.Click += BtnQuickOCR_Click;
            grpAdvancedTools.Controls.Add(_btnQuickOCR);
            _btnDukeCaptcha = CreateModernButton("🎯 Duke", new Point(285, 25), new Size(60, 40), WarningOrange);
            _btnDukeCaptcha.Click += BtnDukeCaptcha_Click;
            grpAdvancedTools.Controls.Add(_btnDukeCaptcha);
        }

        private void InitializeMonitoringGroup()
        {
            _lblStatus = new Label { Text = "Trạng thái: Chưa khởi tạo", Location = new Point(15, 25), Size = new Size(300, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            grpMonitoring.Controls.Add(_lblStatus);
            _lblMonitoringStatus = new Label { Text = "Monitoring: Stopped", Location = new Point(330, 25), Size = new Size(200, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = SecondaryBlue };
            grpMonitoring.Controls.Add(_lblMonitoringStatus);
            _lblPerformanceStats = new Label { Text = "Performance: N/A", Location = new Point(15, 50), Size = new Size(500, 20), Font = new Font("Segoe UI", 9F), ForeColor = DarkGray };
            grpMonitoring.Controls.Add(_lblPerformanceStats);
            _btnStartMonitoring = CreateModernButton("▶️ Monitor", new Point(540, 25), new Size(80, 40), SuccessGreen);
            _btnStartMonitoring.Click += BtnStartMonitoring_Click;
            grpMonitoring.Controls.Add(_btnStartMonitoring);
            _btnStopMonitoring = CreateModernButton("⏹️ Stop", new Point(630, 25), new Size(80, 40), DangerRed);
            _btnStopMonitoring.Click += BtnStopMonitoring_Click;
            _btnStopMonitoring.Enabled = false;
            grpMonitoring.Controls.Add(_btnStopMonitoring);
        }

        private void InitializePreviewGroup()
        {
            _lblImageInfo = new Label { Text = "Hình ảnh: Chưa có", Location = new Point(15, 25), Size = new Size(570, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            grpPreview.Controls.Add(_lblImageInfo);
            _picCaptcha = new PictureBox { Location = new Point(15, 50), Size = new Size(280, 150), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Cursor = Cursors.Hand };
            _picCaptcha.Click += PicCaptcha_Click;
            grpPreview.Controls.Add(_picCaptcha);
            _picGamePreview = new PictureBox { Location = new Point(305, 50), Size = new Size(260, 146), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Normal, BackColor = Color.Black };
            grpPreview.Controls.Add(_picGamePreview);
            var lblCaptchaPreview = new Label { Text = "Captcha Preview", Location = new Point(15, 210), Size = new Size(280, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = DarkGray };
            grpPreview.Controls.Add(lblCaptchaPreview);
            var lblGamePreview = new Label { Text = "Game Window Preview", Location = new Point(305, 210), Size = new Size(280, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = DarkGray };
            grpPreview.Controls.Add(lblGamePreview);
        }

        private void InitializeLogOutputGroup()
        {
            _txtLog = new TextBox { Location = new Point(15, 25), Size = new Size(1310, 340), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 9F), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.FromArgb(220, 220, 220), BorderStyle = BorderStyle.FixedSingle };
            grpLogOutput.Controls.Add(_txtLog);
        }

        private void InitializeTimers()
        {
            _statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
            _previewTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _previewTimer.Tick += PreviewTimer_Tick;
        }

        private Button CreateModernButton(string text, Point location, Size size, Color backColor)
        {
            var button = new Button { Text = text, Location = location, Size = size, BackColor = backColor, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.2f);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.2f);
            button.MouseEnter += (s, e) => button.BackColor = ControlPaint.Light(backColor, 0.2f);
            button.MouseLeave += (s, e) => button.BackColor = backColor;
            return button;
        }

        private void InitializeAutomationService()
        {
            _automationService = new CaptchaAutomationService();
            _automationService.CaptchaDetected += OnCaptchaDetected;
            _automationService.CaptchaProcessed += OnCaptchaProcessed;
            _automationService.ErrorOccurred += OnErrorOccurred;
            _automationService.MonitoringStatusChanged += OnMonitoringStatusChanged;
            _automationService.ResponseReceived += OnResponseReceived;
            _ocrSpaceService = new OCRSpaceService();
            LogMessage("✅ Enhanced Captcha Automation Tool initialized");
            LogMessage("🌐 OCR.space API service initialized");
            LogMessage("🎯 New features: Continuous Monitoring, Response Verification, Enhanced Stats, OCR API");
        }

        private void BtnSelectWindow_Click(object? sender, EventArgs e)
        {
            try
            {
                using (var selector = new GameWindowSelector()) { if (selector.ShowDialog() == DialogResult.OK && selector.SelectedWindow != null) { _selectedGameWindow = selector.SelectedWindow; _lblSelectedWindow.Text = "Cửa sổ game: "; _lblSelectedWindow.ForeColor = SuccessGreen; LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle} ({_selectedGameWindow.Bounds.Width}x{_selectedGameWindow.Bounds.Height})"); StartGameWindowPreview(); UpdateGameWindowPreview(); LogMessage("✅ Đã bắt đầu preview real-time cho cửa sổ game"); } }
            }
            catch (Exception ex) { LogMessage($"Lỗi khi chọn cửa sổ: {ex.Message}"); }
        }
        
        private void StartGameWindowPreview()
        {
            try
            {
                if (_selectedGameWindow != null && _selectedGameWindow.IsValid() && _previewTimer != null)
                {
                    if (_previewTimer.Enabled) { _previewTimer.Stop(); }
                    _previewTimer.Tick -= PreviewTimer_Tick;
                    _previewTimer.Interval = 500;
                    _previewTimer.Tick += PreviewTimer_Tick;
                    _previewTimer.Start();
                    Console.WriteLine("✅ Started game window preview timer (500ms interval)");
                    LogMessage("✅ Đã bắt đầu preview real-time cửa sổ game");
                    UpdateGameWindowPreview();
                }
                else
                {
                    Console.WriteLine("❌ Cannot start preview: Invalid game window or timer");
                    LogMessage("❌ Không thể bắt đầu preview: Cửa sổ game không hợp lệ hoặc timer null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting game window preview: {ex.Message}");
                LogMessage($"❌ Lỗi bắt đầu preview: {ex.Message}");
            }
        }
        
        private void StopGameWindowPreview()
        {
            _previewTimer.Stop();
            _previewTimer.Tick -= PreviewTimer_Tick;
            LogMessage("Đã dừng preview cửa sổ game");
        }
        
        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            UpdateGameWindowPreview();
        }
        
        private void UpdateGameWindowPreview()
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid()) return;
            try
            {
                var bounds = _selectedGameWindow.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) return;
                var handle = _selectedGameWindow.Handle;
                if (handle == IntPtr.Zero) return;

                using (Bitmap? capture = ScreenCapture.CaptureWindowClientArea(handle, new Rectangle(0, 0, bounds.Width, bounds.Height)))
                {
                    if (capture != null && capture.Width > 1 && capture.Height > 1)
                    {
                        var previewBmp = FitTo(new BoxSize(260, 146), capture);
                        var oldImage = _picGamePreview.Image;
                        _picGamePreview.Image = previewBmp;
                        oldImage?.Dispose();
                        _lblImageInfo.Text = $"Game Preview: {bounds.Width}x{bounds.Height} | Display: {previewBmp.Width}x{previewBmp.Height}";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating preview: {ex.Message}");
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                _btnStart.Enabled = false;
                _btnStop.Enabled = true;
                _lblStatus.Text = "Trạng thái: Đang khởi tạo...";
                _lblStatus.ForeColor = Color.Orange;

                using (var selector = new GameWindowSelector()) { if (selector.ShowDialog() == DialogResult.OK) { _selectedGameWindow = selector.SelectedWindow; if (_selectedGameWindow != null) { _lblSelectedWindow.Text = "Cửa sổ game: "; _lblSelectedWindow.ForeColor = Color.Green; LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}"); LogMessage($"📏 Kích thước: {_selectedGameWindow.Bounds.Width}x{_selectedGameWindow.Bounds.Height}"); StartGameWindowPreview(); } else { LogMessage("Không thể lấy thông tin cửa sổ game"); _lblStatus.Text = "Trạng thái: Lỗi chọn cửa sổ"; _lblStatus.ForeColor = Color.Red; _btnStart.Enabled = true; _btnStop.Enabled = false; return; } } else { _lblStatus.Text = "Trạng thái: Chưa chọn cửa sổ game"; _lblStatus.ForeColor = Color.Red; _btnStart.Enabled = true; _btnStop.Enabled = false; return; } }

                if (!_automationService.Initialize(_selectedGameWindow)) { _lblStatus.Text = "Trạng thái: Lỗi khởi tạo"; _lblStatus.ForeColor = Color.Red; _btnStart.Enabled = true; _btnStop.Enabled = false; return; }

                _lblStatus.Text = "Trạng thái: Đang chạy...";
                _lblStatus.ForeColor = SuccessGreen;

                _automationService.StartMonitoring();
                
                await Task.Run(async () => { try { while (_btnStop.Enabled) { try { await this.InvokeAsync(() => LogMessage("🔄 Đang xử lý captcha...")); bool result = await _automationService.ProcessCaptchaAsync(); await this.InvokeAsync(() => LogMessage($"📊 Kết quả xử lý: {(result ? "Thành công" : "Thất bại")}")); await Task.Delay(2000); } catch (Exception ex) { await this.InvokeAsync(() => LogMessage($"❌ Lỗi trong vòng lặp: {ex.Message}")); await this.InvokeAsync(() => LogMessage($"❌ Stack trace: {ex.StackTrace}")); await Task.Delay(5000); } } } catch (Exception ex) { await this.InvokeAsync(() => { LogMessage($"Lỗi trong automation task: {ex.Message}"); _lblStatus.Text = "Trạng thái: Lỗi"; _lblStatus.ForeColor = Color.Red; _btnStart.Enabled = true; _btnStop.Enabled = false; }); } });
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi bắt đầu automation: {ex.Message}");
                _lblStatus.Text = "Trạng thái: Lỗi";
                _lblStatus.ForeColor = Color.Red;
                _btnStart.Enabled = true;
                _btnStop.Enabled = false;
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            _lblStatus.Text = "Trạng thái: Đã dừng";
            _lblStatus.ForeColor = SecondaryBlue;
            LogMessage("Đã dừng automation");
            _automationService.StopMonitoring();
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Đang test OCR...");
                if (_selectedGameWindow == null)
                {
                    using (var selector = new GameWindowSelector()) { if (selector.ShowDialog() == DialogResult.OK) { _selectedGameWindow = selector.SelectedWindow; if (_selectedGameWindow != null) { _lblSelectedWindow.Text = "Cửa sổ game: "; _lblSelectedWindow.ForeColor = Color.Green; LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}"); } else { LogMessage("Không thể lấy thông tin cửa sổ game"); } } else { LogMessage("Đã hủy chọn cửa sổ. Không thể test."); return; } }
                }

                if (!_automationService.Initialize(_selectedGameWindow)) { LogMessage("Không thể khởi tạo để test"); return; }

                bool result = await Task.Run(() => _automationService.ProcessCaptchaAsync());
                if (result) { LogMessage("✅ Test OCR thành công!"); } else { LogMessage("❌ Test OCR thất bại."); }
            }
            catch (Exception ex) { LogMessage($"Lỗi test: {ex.Message}"); }
        }

        private void BtnDebug_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("=== DEBUG: Tìm cửa sổ game ===");
                LogMessage(WindowFinder.FindGameWindows());
                LogMessage("=== DEBUG: Tất cả cửa sổ ===");
                LogMessage(WindowFinder.FindAllWindows());
            }
            catch (Exception ex) { LogMessage($"Lỗi debug: {ex.Message}"); }
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (_automationService != null)
            {
                _lblStats.Text = $"Thành công: {_automationService.SuccessCount} | Thất bại: {_automationService.FailureCount}";
                if (!string.IsNullOrEmpty(_automationService.LastCaptchaText))
                {
                    _lblCaptcha.Text = $"Captcha: {_automationService.LastCaptchaText}";
                }
                try
                {
                    var perfStats = _automationService.GetPerformanceStats();
                    string perfText = $"Success Rate: {perfStats.SuccessRate:F1}% | ";
                    if (perfStats.MonitoringStats != null)
                    {
                        perfText += $"Checks: {perfStats.MonitoringStats.TotalChecks} | ";
                        perfText += $"Detection Rate: {perfStats.MonitoringStats.DetectionRate:F1}% | ";
                        perfText += $"Interval: {perfStats.MonitoringStats.CurrentInterval}ms";
                    }
                    _lblPerformanceStats.Text = perfText;
                }
                catch (Exception ex)
                {
                    _lblPerformanceStats.Text = $"Performance: Error - {ex.Message}";
                }
            }
        }

        private void OnCaptchaDetected(object? sender, string captchaText)
        {
            try
            {
                if (this.InvokeRequired) { this.Invoke(new Action(() => OnCaptchaDetected(sender, captchaText))); return; }
                LogMessage(string.IsNullOrEmpty(captchaText) ? "Captcha được phát hiện: (không có text)" : $("Captcha được phát hiện: {captchaText}"));
            }
            catch (Exception ex) { Console.WriteLine($"❌ Error in OnCaptchaDetected: {ex.Message}"); }
        }

        private void OnCaptchaProcessed(object? sender, string captchaText)
        {
            try
            {
                if (this.InvokeRequired) { this.Invoke(new Action(() => OnCaptchaProcessed(sender, captchaText))); return; }
                LogMessage(string.IsNullOrEmpty(captchaText) ? "Captcha đã được xử lý: (không có text)" : $("Captcha đã được xử lý: {captchaText}"));
            }
            catch (Exception ex) { Console.WriteLine($"❌ Error in OnCaptchaProcessed: {ex.Message}"); }
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            try
            {
                if (this.InvokeRequired) { this.Invoke(new Action(() => OnErrorOccurred(sender, error))); return; }
                LogMessage($"Lỗi: {error}");
            }
            catch (Exception ex) { Console.WriteLine($"❌ Error in OnErrorOccurred: {ex.Message}"); }
        }

        private void LogMessage(string message)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => LogMessage(message))); return; }
            if (_txtLog == null) { Console.WriteLine($"[LOG] {message}"); return; }
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                _txtLog.AppendText($"[{timestamp}] {message}\r\n");
                _txtLog.SelectionStart = _txtLog.Text.Length;
                _txtLog.ScrollToCaret();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LogMessage: {ex.Message}");
                Console.WriteLine($"Original message: {message}");
            }
        }

        private void BtnStartMonitoring_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_isMonitoring) { LogMessage("Monitoring đã đang chạy."); return; }
                using (var selector = new GameWindowSelector()) { if (selector.ShowDialog() == DialogResult.OK && selector.SelectedWindow != null) { _selectedGameWindow = selector.SelectedWindow; _lblSelectedWindow.Text = "Cửa sổ game: "; _lblSelectedWindow.ForeColor = SuccessGreen; LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}"); LogMessage($"📏 Kích thước: {_selectedGameWindow.Bounds.Width}x{_selectedGameWindow.Bounds.Height}"); StartGameWindowPreview(); } else { LogMessage("Cần chọn cửa sổ game trước khi start monitoring"); return; } }

                if (!_automationService.Initialize(_selectedGameWindow)) { LogMessage("❌ Không thể khởi tạo automation service"); return; }

                _monitoringCts = new CancellationTokenSource();
                _monitoringTask = Task.Run(() => MonitoringLoop(_monitoringCts.Token));
                _isMonitoring = true;
                _btnStartMonitoring.Enabled = false;
                _btnStopMonitoring.Enabled = true;
                _lblStatus.Text = "Trạng thái: Monitoring - đang giám sát captcha";
                _lblStatus.ForeColor = Color.Green;
                LogMessage("🎯 Đã bắt đầu continuous monitoring.");
            }
            catch (Exception ex) { LogMessage($"❌ Lỗi start monitoring: {ex.Message}"); }
        }

        private async Task MonitoringLoop(CancellationToken token)
        {
            LogMessage("🔄 Vòng lặp monitoring bắt đầu.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_selectedGameWindow == null || !_selectedGameWindow.IsValid()) { await this.InvokeAsync(() => LogMessage("⚠️ Cửa sổ game không hợp lệ.")); await Task.Delay(2000, token); continue; }
                    await this.InvokeAsync(() => UpdateGameWindowPreview());
                    bool captchaDetected = await _automationService.CheckForCaptchaAsync();
                    if (captchaDetected)
                    {
                        await this.InvokeAsync(() => LogMessage("🔍 Phát hiện captcha! Đang xử lý..."));
                        bool result = await _automationService.ProcessCaptchaAsync();
                        await this.InvokeAsync(() => LogMessage($"📊 Kết quả xử lý: {(result ? "Thành công" : "Thất bại")}"));
                    }
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    await this.InvokeAsync(() => LogMessage($"❌ Lỗi trong monitoring loop: {ex.Message}"));
                    await Task.Delay(3000, token);
                }
            }
            await this.InvokeAsync(() => LogMessage("✅ Vòng lặp monitoring đã kết thúc."));
        }

        private async void BtnStopMonitoring_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_monitoringCts != null)
                {
                    LogMessage("🛑 Đang gửi yêu cầu dừng monitoring...");
                    _monitoringCts.Cancel();
                    if (_monitoringTask != null) { await _monitoringTask; }
                    _monitoringCts.Dispose();
                    _monitoringCts = null;
                    _monitoringTask = null;
                    _isMonitoring = false;
                }
                _automationService?.StopContinuousMonitoring();
                _btnStartMonitoring.Enabled = true;
                _btnStopMonitoring.Enabled = false;
                _lblStatus.Text = "Trạng thái: Monitoring stopped";
                _lblStatus.ForeColor = Color.Blue;
                _lblMonitoringStatus.Text = "Monitoring: Stopped";
                _lblMonitoringStatus.ForeColor = Color.Blue;
                LogMessage("⏸️ Đã dừng continuous monitoring.");
            }
            catch (Exception ex) { LogMessage($"❌ Lỗi stop monitoring: {ex.Message}"); }
        }

        private void OnMonitoringStatusChanged(object? sender, string status)
        {
            try
            {
                if (this.InvokeRequired) { this.Invoke(new Action(() => OnMonitoringStatusChanged(sender, status))); return; }
                _lblMonitoringStatus.Text = $"Monitoring: {status}";
                _lblMonitoringStatus.ForeColor = status.Contains("❌") ? Color.Red : status.Contains("✅") ? Color.Green : status.Contains("🔍") ? Color.Orange : Color.Blue;
                LogMessage($"📊 {status}");
            }
            catch (Exception ex) { Console.WriteLine($"❌ Error in OnMonitoringStatusChanged: {ex.Message}"); }
        }

        private void OnResponseReceived(object? sender, CaptchaResponseResult result)
        {
            try
            {
                if (this.InvokeRequired) { this.Invoke(new Action(() => OnResponseReceived(sender, result))); return; }
                string statusText = result.IsSuccess ? "✅ SUCCESS" : $("❌ {result.Status}");
                Color statusColor = result.IsSuccess ? Color.Green : Color.Red;
                LogMessage($"📨 Response: {statusText} - {result.Message}");
                if (result.IsSuccess) { _lblCaptcha.ForeColor = Color.Green; } else { _lblCaptcha.ForeColor = Color.Red; }
            }
            catch (Exception ex) { Console.WriteLine($"❌ Error in OnResponseReceived: {ex.Message}"); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _monitoringCts?.Cancel();
                if (_monitoringTask != null && !_monitoringTask.IsCompleted) { if (!_monitoringTask.Wait(3000)) { LogMessage("⚠️ Monitoring task không dừng kịp thời."); } }
                _monitoringCts?.Dispose();
                _statusTimer?.Stop();
                _previewTimer?.Stop();
                if (_statusTimer != null) _statusTimer.Tick -= StatusTimer_Tick;
                if (_previewTimer != null) _previewTimer.Tick -= PreviewTimer_Tick;
                _automationService?.StopContinuousMonitoring();
                _automationService?.Dispose();
                _ocrSpaceService?.Dispose();
                _picCaptcha?.Image?.Dispose();
                _picGamePreview?.Image?.Dispose();
                _statusTimer?.Dispose();
                _previewTimer?.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex) { Console.WriteLine($"Error during form closing: {ex.Message}"); }
            base.OnFormClosing(e);
        }

        private void BtnCaptureDebug_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("📸 Bắt đầu capture debug...");
                if (_selectedGameWindow == null)
                {
                    using (var selector = new GameWindowSelector()) { if (selector.ShowDialog() == DialogResult.OK) { _selectedGameWindow = selector.SelectedWindow; if (_selectedGameWindow != null) { _lblSelectedWindow.Text = "Cửa sổ game: "; _lblSelectedWindow.ForeColor = Color.Green; LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}"); } else { LogMessage("Không thể lấy thông tin cửa sổ game"); } } else { LogMessage("Đã hủy chọn cửa sổ. Không thể capture debug."); return; } }
                }
                if (!_automationService.Initialize(_selectedGameWindow)) { LogMessage("Không thể khởi tạo để capture debug"); return; }
                _automationService.RunCaptureDebug();
                string debugFolder = "debug_captures";
                if (Directory.Exists(debugFolder))
                {
                    var files = Directory.GetFiles(debugFolder, "*.png").OrderByDescending(f => File.GetCreationTime(f)).ToArray();
                    if (files.Length > 0)
                    {
                        var fullWindowFile = files.FirstOrDefault(f => f.Contains("full_window"));
                        if (fullWindowFile != null) { DisplayImage(fullWindowFile, "Toàn bộ cửa sổ game"); LogMessage("🖼️ Hiển thị full window để phân tích vị trí captcha"); }
                        else { DisplayImage(files[0], "Hình ảnh đã capture"); }
                        LogMessage($"✅ Đã lưu {files.Length} file hình ảnh");
                        LogMessage("📷 Kiểm tra hình ảnh để xác định vị trí captcha chính xác");
                        LogMessage("🔍 Click vào hình ảnh để xem to hơn");
                    }
                }
            }
            catch (Exception ex) { LogMessage($"❌ Lỗi capture debug: {ex.Message}"); }
        }

        private void DisplayImage(string imagePath, string description)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read)) { using (var image = Image.FromStream(fs)) { _picCaptcha.Image?.Dispose(); _picCaptcha.Image = new Bitmap(image); } }
                    _lblImageInfo.Text = $"Hình ảnh: {description} ({Path.GetFileName(imagePath)})";
                    _lblImageInfo.ForeColor = Color.Green;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi hiển thị hình ảnh: {ex.Message}");
                _lblImageInfo.Text = "Lỗi hiển thị hình ảnh";
                _lblImageInfo.ForeColor = Color.Red;
            }
        }

        private void PicCaptcha_Click(object? sender, EventArgs e)
        {
            if (_picCaptcha.Image != null)
            {
                try
                {
                    var imageViewer = new Form { Text = "Xem hình ảnh toàn màn hình", Size = new Size(Math.Max(600, _picCaptcha.Image.Width + 50), Math.Max(400, _picCaptcha.Image.Height + 100)), StartPosition = FormStartPosition.CenterParent };
                    Image imageCopy = new Bitmap(_picCaptcha.Image);
                    var picFull = new PictureBox { Image = imageCopy, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
                    imageViewer.FormClosed += (s, args) => { picFull.Image?.Dispose(); picFull.Image = null; };
                    imageViewer.Controls.Add(picFull);
                    imageViewer.ShowDialog();
                }
                catch (Exception ex) { Console.WriteLine($"Error displaying full image: {ex.Message}"); }
            }
        }

        private async void BtnCaptureProcess_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("📸 Bắt đầu Capture & Process workflow...");
                if (_selectedGameWindow == null) { LogMessage("❌ Chưa chọn cửa sổ game. Vui lòng chọn cửa sổ trước."); return; }
                if (!_selectedGameWindow.IsValid()) { LogMessage("❌ Cửa sổ game không hợp lệ. Vui lòng chọn lại cửa sổ."); return; }
                var bounds = _selectedGameWindow.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) { LogMessage("❌ Kích thước cửa sổ game không hợp lệ."); return; }
                var handle = _selectedGameWindow.Handle;
                if (handle == IntPtr.Zero) { LogMessage("❌ Handle cửa sổ game không hợp lệ."); return; }
                await Task.Delay(1);
                LogMessage("🖼️ Bước 1: Capture toàn bộ cửa sổ game...");
                Rectangle fullWindowArea = new Rectangle(0, 0, bounds.Width, bounds.Height);
                using (Bitmap? fullWindowCapture = ScreenCapture.CaptureWindow(handle, fullWindowArea))
                {
                    if (fullWindowCapture == null) { LogMessage("❌ Không thể capture cửa sổ game"); return; }
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    string fullWindowPath = Path.Combine("captcha_workflow", $
                    