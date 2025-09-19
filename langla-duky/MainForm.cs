using System;
using System.Drawing;
using System.Windows.Forms;
using langla_duky.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Tesseract;
using System.Drawing.Imaging;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

public static class ControlExtensions
{
    public static Task InvokeAsync(this Control control, Action action)
    {
        var tcs = new TaskCompletionSource<object?>();
        if (control.InvokeRequired)
        {
            try
            {
                control.BeginInvoke(new Action(() =>
                {
                    try { action(); tcs.TrySetResult(null); } 
                    catch (Exception ex) { tcs.TrySetException(ex); }
                }));
            }
            catch (Exception ex) 
            { 
                tcs.TrySetException(ex); 
            }
        }
        else
        {
            try { action(); tcs.TrySetResult(null); } 
            catch (Exception ex) { tcs.TrySetException(ex); }
        }
        return tcs.Task;
    }
}

namespace langla_duky
{
    public partial class MainForm : Form
    {
        private Config _config = null!;
        private TesseractEngine? _tessEngine;
        private int _successCount = 0;
        private int _failureCount = 0;
        private string _lastCaptchaText = string.Empty;
        private string _lastCaptchaImagePath = string.Empty;
        private Rectangle _lastCapturedArea = Rectangle.Empty;
        private float _lastPreviewScale = 1.0f;
        private int _lastPreviewOffsetX = 0;
        private int _lastPreviewOffsetY = 0;
        private Size _lastCaptureSize = Size.Empty;
        private ManualCaptchaCapture _manualCapture = null!;
        private enum SelectionMode { None, Area, InputField, ConfirmButton }
        private SelectionMode _selectionMode = SelectionMode.None;
        private bool _isSelecting = false;
        private Point _selectionStart;
        private Rectangle _selectionRect = Rectangle.Empty;

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
        private Button _btnSetCaptchaArea = null!;
        private Button _btnSetInputField = null!;
        private Button _btnSetConfirmButton = null!;

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
        private CheckBox _chkUseManual = null!;

        private System.Windows.Forms.Timer _statusTimer = null!;
        private System.Windows.Forms.Timer _previewTimer = null!;
        private System.Timers.Timer _watchdogTimer = null!;
        private GameWindow? _selectedGameWindow;
        
        private CancellationTokenSource? _monitoringCts;
        private Task? _monitoringTask;
        private volatile bool _isMonitoring = false;
        private DateTime _lastPreviewUpdateTime = DateTime.MinValue;
        private readonly bool _enableDebugOutput = true;
        private string _lastOcrMethod = string.Empty;
        private string _lastRoiMode = string.Empty;
        private bool _suppressManualEvents = false;

        private static readonly Color PrimaryBlue = Color.FromArgb(41, 128, 185);
        private static readonly Color SecondaryBlue = Color.FromArgb(52, 152, 219);
        private static readonly Color SuccessGreen = Color.FromArgb(39, 174, 96);
        private static readonly Color WarningOrange = Color.FromArgb(230, 126, 34);
        private static readonly Color DangerRed = Color.FromArgb(231, 76, 60);
        private static readonly Color LightGray = Color.FromArgb(236, 240, 241);
        private static readonly Color DarkGray = Color.FromArgb(52, 73, 94);

        public struct BoxSize { public int Width { get; } public int Height { get; } public BoxSize(int width, int height) { Width = width; Height = height; } }

        public MainForm()
        {
            InitializeComponent();
            _config = Config.LoadFromFile();
            _manualCapture = new ManualCaptchaCapture(_config);
            InitializeTesseract();
            _suppressManualEvents = true;
            UpdateUIForWindowState();
            _suppressManualEvents = false;
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
            this.Text = "Captcha Automation Tool";
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
            grpGameWindow = new GroupBox { Text = "Game Window", Location = new Point(20, 20), Size = new Size(350, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpGameWindow);
            grpCaptchaInfo = new GroupBox { Text = "Captcha Information", Location = new Point(390, 20), Size = new Size(350, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpCaptchaInfo);
            grpMainControls = new GroupBox { Text = "Main Controls", Location = new Point(20, 160), Size = new Size(350, 100), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpMainControls);
            grpAdvancedTools = new GroupBox { Text = "Advanced Tools", Location = new Point(390, 160), Size = new Size(500, 100), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            this.Controls.Add(grpAdvancedTools);
            grpMonitoring = new GroupBox { Text = "Monitoring & Status", Location = new Point(20, 280), Size = new Size(720, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpMonitoring);
            grpPreview = new GroupBox { Text = "Preview & Images", Location = new Point(760, 20), Size = new Size(600, 380), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpPreview);
            grpLogOutput = new GroupBox { Text = "Log Output", Location = new Point(20, 420), Size = new Size(1340, 380), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = PrimaryBlue };
            this.Controls.Add(grpLogOutput);
        }

        private void InitializeGameWindowGroup()
        {
            _lblSelectedWindow = new Label { Text = "Game Window: Not Selected", Location = new Point(15, 25), Size = new Size(320, 20), Font = new Font("Segoe UI", 9F), ForeColor = DangerRed };
            grpGameWindow.Controls.Add(_lblSelectedWindow);
            _btnSelectWindow = CreateModernButton("Select Window", new Point(15, 55), new Size(150, 35), WarningOrange);
            _btnSelectWindow.Click += BtnSelectWindow_Click;
            grpGameWindow.Controls.Add(_btnSelectWindow);
            var btnAutoDetect = CreateModernButton("Auto-Detect", new Point(180, 55), new Size(150, 35), SecondaryBlue);
            btnAutoDetect.Click += async (s, e) => await AutoDetectGameWindow();
            grpGameWindow.Controls.Add(btnAutoDetect);
        }

        private void InitializeCaptchaInfoGroup()
        {
            _lblCaptcha = new Label { Text = "Captcha: None", Location = new Point(15, 25), Size = new Size(320, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            grpCaptchaInfo.Controls.Add(_lblCaptcha);
            _lblStats = new Label { Text = "Success: 0 | Fail: 0", Location = new Point(15, 50), Size = new Size(320, 20), Font = new Font("Segoe UI", 9F) };
            grpCaptchaInfo.Controls.Add(_lblStats);
            _chkUseOCRAPI = new CheckBox { Text = "OpenCV binarize", Location = new Point(15, 75), Size = new Size(200, 25), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = SecondaryBlue, Checked = true };
            _chkUseOCRAPI.CheckedChanged += (s, e) => { LogMessage($"🔧 OpenCV binarize: {(_chkUseOCRAPI.Checked ? "Enabled" : "Disabled")}"); };
            grpCaptchaInfo.Controls.Add(_chkUseOCRAPI);
        }

        private void InitializeMainControlsGroup()
        {
            _btnStart = CreateModernButton("Start", new Point(15, 25), new Size(100, 40), SuccessGreen);
            _btnStart.Click += BtnStartMonitoring_Click; // Changed to unified monitoring handler
            grpMainControls.Controls.Add(_btnStart);
            _btnStop = CreateModernButton("Stop", new Point(130, 25), new Size(100, 40), DangerRed);
            _btnStop.Click += BtnStopMonitoring_Click; // Changed to unified monitoring handler
            grpMainControls.Controls.Add(_btnStop);
            _btnTest = CreateModernButton("Test OCR", new Point(245, 25), new Size(90, 40), SecondaryBlue);
            _btnTest.Click += BtnTest_Click;
            grpMainControls.Controls.Add(_btnTest);
        }

        private void InitializeAdvancedToolsGroup()
        {
            _btnDebug = CreateModernButton("Debug", new Point(15, 25), new Size(80, 40), WarningOrange);
            _btnDebug.Click += BtnDebug_Click;
            grpAdvancedTools.Controls.Add(_btnDebug);

            _btnCaptureDebug = CreateModernButton("Capture", new Point(100, 25), new Size(80, 40), WarningOrange);
            _btnCaptureDebug.Click += BtnCaptureDebug_Click;
            grpAdvancedTools.Controls.Add(_btnCaptureDebug);

            _btnSetCaptchaArea = CreateModernButton("Set Captcha", new Point(185, 25), new Size(100, 40), SecondaryBlue);
            _btnSetCaptchaArea.Click += BtnSetCaptchaArea_Click;
            grpAdvancedTools.Controls.Add(_btnSetCaptchaArea);

            _btnSetInputField = CreateModernButton("Set Input", new Point(290, 25), new Size(100, 40), SecondaryBlue);
            _btnSetInputField.Click += BtnSetInputField_Click;
            grpAdvancedTools.Controls.Add(_btnSetInputField);

            _btnSetConfirmButton = CreateModernButton("Set Confirm", new Point(395, 25), new Size(110, 40), SecondaryBlue);
            _btnSetConfirmButton.Click += BtnSetConfirmButton_Click;
            grpAdvancedTools.Controls.Add(_btnSetConfirmButton);

            var btnDebugROI = CreateModernButton("Debug ROI", new Point(510, 25), new Size(100, 40), WarningOrange);
            btnDebugROI.Click += BtnDebugROI_Click;
            grpAdvancedTools.Controls.Add(btnDebugROI);

            var btnTestROI = CreateModernButton("Test ROI", new Point(615, 25), new Size(100, 40), SuccessGreen);
            btnTestROI.Click += BtnTestROI_Click;
            grpAdvancedTools.Controls.Add(btnTestROI);

            var btnDebugFullWindow = CreateModernButton("Debug Full", new Point(720, 25), new Size(100, 40), PrimaryBlue);
            btnDebugFullWindow.Click += BtnDebugFullWindow_Click;
            grpAdvancedTools.Controls.Add(btnDebugFullWindow);

            _chkUseManual = new CheckBox { Text = "Use Manual", Location = new Point(15, 70), Size = new Size(120, 25), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = PrimaryBlue, Checked = false };
            _chkUseManual.CheckedChanged += (s, e) =>
            {
                LogMessage($"DEBUG: CheckedChanged event triggered, suppress={_suppressManualEvents}, newVal={_chkUseManual.Checked}");
                if (_suppressManualEvents) return;
                bool newVal = _chkUseManual.Checked;
                if (_config?.UseManualCapture == newVal)
                {
                    // No change; do nothing
                    return;
                }
                if (_config != null)
                {
                    _config.UseManualCapture = newVal;
                    _config.SaveToFile();
                    LogMessage($"DEBUG: Saved UseManualCapture={newVal} to config");
                }
                _btnSetCaptchaArea.Enabled = newVal;
                _btnSetInputField.Enabled = newVal;
                _btnSetConfirmButton.Enabled = newVal;
                LogMessage($"Manual mode {(newVal ? "enabled" : "disabled")} and saved to config.json");
            };
            grpAdvancedTools.Controls.Add(_chkUseManual);
        }

        private void InitializeMonitoringGroup()
        {
            _lblStatus = new Label { Text = "Status: Idle", Location = new Point(15, 25), Size = new Size(300, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            grpMonitoring.Controls.Add(_lblStatus);
            _lblMonitoringStatus = new Label { Text = "Monitoring: Stopped", Location = new Point(330, 25), Size = new Size(200, 20), Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = SecondaryBlue };
            grpMonitoring.Controls.Add(_lblMonitoringStatus);
            _lblPerformanceStats = new Label { Text = "Performance: N/A", Location = new Point(15, 50), Size = new Size(500, 20), Font = new Font("Segoe UI", 9F), ForeColor = DarkGray };
            grpMonitoring.Controls.Add(_lblPerformanceStats);
            _btnStartMonitoring = CreateModernButton("Start Monitor", new Point(540, 25), new Size(80, 40), SuccessGreen);
            _btnStartMonitoring.Click += BtnStartMonitoring_Click;
            grpMonitoring.Controls.Add(_btnStartMonitoring);
            _btnStopMonitoring = CreateModernButton("Stop Monitor", new Point(630, 25), new Size(80, 40), DangerRed);
            _btnStopMonitoring.Click += BtnStopMonitoring_Click;
            grpMonitoring.Controls.Add(_btnStopMonitoring);
        }

        private void InitializePreviewGroup()
        {
            _lblImageInfo = new Label { Text = "Image Info: None", Location = new Point(15, 25), Size = new Size(570, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            grpPreview.Controls.Add(_lblImageInfo);
            _picCaptcha = new PictureBox { Location = new Point(15, 50), Size = new Size(280, 150), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White, Cursor = Cursors.Hand };
            _picCaptcha.Click += PicCaptcha_Click;
            grpPreview.Controls.Add(_picCaptcha);
            _picGamePreview = new PictureBox { Location = new Point(305, 50), Size = new Size(260, 146), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Normal, BackColor = Color.Black };
            _picGamePreview.MouseDown += PicGamePreview_MouseDown;
            _picGamePreview.MouseMove += PicGamePreview_MouseMove;
            _picGamePreview.MouseUp += PicGamePreview_MouseUp;
            _picGamePreview.Paint += PicGamePreview_Paint;
            grpPreview.Controls.Add(_picGamePreview);
            var lblCaptchaPreview = new Label { Text = "Captcha Preview", Location = new Point(15, 210), Size = new Size(280, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = DarkGray };
            grpPreview.Controls.Add(lblCaptchaPreview);
            var lblGamePreview = new Label { Text = "Game Window Preview", Location = new Point(305, 210), Size = new Size(280, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = DarkGray };
            grpPreview.Controls.Add(lblGamePreview);
        }

        private void InitializeLogOutputGroup()
        {
            _txtLog = new TextBox { Location = new Point(15, 25), Size = new Size(1310, 340), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Segoe UI", 9F), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.FromArgb(220, 220, 220), BorderStyle = BorderStyle.FixedSingle };
            grpLogOutput.Controls.Add(_txtLog);
        }

        private void InitializeTimers()
        {
            _statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();

            _previewTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _previewTimer.Tick += PreviewTimer_Tick;

            _watchdogTimer = new System.Timers.Timer(5000);
            _watchdogTimer.Elapsed += Watchdog_Elapsed;
            _watchdogTimer.AutoReset = true;
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

        private void InitializeTesseract()
        {
            try
            {
                _tessEngine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
                _tessEngine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                // Try different page segmentation modes for better CAPTCHA recognition
                _tessEngine.SetVariable("tessedit_pageseg_mode", "8"); // Single word
                _tessEngine.SetVariable("tessedit_ocr_engine_mode", "1"); // Neural nets LSTM only
                _tessEngine.SetVariable("classify_bln_numeric_mode", "0");
                // Additional settings for better CAPTCHA recognition
                _tessEngine.SetVariable("tessedit_char_blacklist", "!@#$%^&*()_+-=[]{}|;':\",./<>?`~");
                _tessEngine.SetVariable("tessedit_do_invert", "0");
                _tessEngine.SetVariable("textord_min_linesize", "2.5");
                // Settings for colored text recognition
                _tessEngine.SetVariable("tessedit_char_blacklist", "");
                _tessEngine.SetVariable("textord_min_xheight", "8");
                _tessEngine.SetVariable("textord_min_linesize", "1.5");
                _tessEngine.SetVariable("tessedit_pageseg_mode", "6"); // Uniform block of text
                LogMessage("✅ Initialized Tesseract engine with colored CAPTCHA-optimized settings");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Failed to initialize Tesseract: {ex.Message}");
            }
        }

        private void BtnSelectWindow_Click(object? sender, EventArgs e)
        {
            try
            {
                using (var selector = new GameWindowSelector()) 
                {
                    if (selector.ShowDialog() == DialogResult.OK && selector.SelectedWindow != null) 
                    {
                        _selectedGameWindow = selector.SelectedWindow;
                        LogMessage($"Selected window: {_selectedGameWindow.WindowTitle} ({_selectedGameWindow.Bounds.Width}x{_selectedGameWindow.Bounds.Height})");
                        StartGameWindowPreview();
                        _suppressManualEvents = true;
                        UpdateUIForWindowState();
                        _suppressManualEvents = false;
                    }
                }
            }
            catch (Exception ex) { LogMessage($"Error selecting window: {ex.Message}"); }
        }
        
        private void StartGameWindowPreview()
        {
            try
            {
                if (_selectedGameWindow != null && _selectedGameWindow.IsValid() && _previewTimer != null)
                {
                    if (_previewTimer.Enabled) _previewTimer.Stop();
                    _previewTimer.Start();
                    // Initialize preview timestamp to avoid early watchdog false positives
                    _lastPreviewUpdateTime = DateTime.UtcNow;
                    LogMessage("Game window preview started.");
                }
            }
            catch (Exception ex) { LogMessage($"Error starting preview: {ex.Message}"); }
        }
        
        private void StopGameWindowPreview() { _previewTimer.Stop(); LogMessage("Game window preview stopped."); }
        
        private void PreviewTimer_Tick(object? sender, EventArgs e) 
        {
             _ = UpdateGameWindowPreviewAsync();
        }
        
        private async Task UpdateGameWindowPreviewAsync()
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid()) return;

            try
            {
                Bitmap? capture = await Task.Run(() => 
                {
                    var bounds = _selectedGameWindow.Bounds;
                    if (bounds.Width <= 0 || bounds.Height <= 0) return null;
                    return ScreenCapture.CaptureWindowClientArea(_selectedGameWindow.Handle, bounds);
                });

                if (capture != null)
                {
                    await this.InvokeAsync(() =>
                    {
                        // Calculate FitTo parameters for reverse mapping
                        int targetW = _picGamePreview.Width;
                        int targetH = _picGamePreview.Height;
                        float scale = Math.Min((float)targetW / capture.Width, (float)targetH / capture.Height);
                        int newWidth = (int)(capture.Width * scale);
                        int newHeight = (int)(capture.Height * scale);
                        int offX = (targetW - newWidth) / 2;
                        int offY = (targetH - newHeight) / 2;
                        var previewBmp = new Bitmap(targetW, targetH);
                        previewBmp.SetResolution(capture.HorizontalResolution, capture.VerticalResolution);
                        using (var g = Graphics.FromImage(previewBmp))
                        {
                            g.Clear(Color.Black);
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(capture, new Rectangle(offX, offY, newWidth, newHeight));
                        }
                        var oldImage = _picGamePreview.Image;
                        _picGamePreview.Image = previewBmp;
                        oldImage?.Dispose();
                        _lblImageInfo.Text = $"Preview: {capture.Width}x{capture.Height} | Display: {previewBmp.Width}x{previewBmp.Height}";
                        _lastPreviewUpdateTime = DateTime.UtcNow;
                        _lastPreviewScale = scale;
                        _lastPreviewOffsetX = offX;
                        _lastPreviewOffsetY = offY;
                        _lastCaptureSize = new Size(capture.Width, capture.Height);
                    });
                    capture.Dispose();
                }
            }
            catch (Exception ex) { await this.InvokeAsync(() => LogMessage($"Error updating preview: {ex.Message}")); }
        }

        private async void BtnStartMonitoring_Click(object? sender, EventArgs e)
        {
            // Prevent multiple concurrent executions
            if (_isMonitoring) { LogMessage("Monitoring is already running."); return; }

            // Always reload latest config from file so Start reflects @config.json changes
            try
            {
                LogMessage($"DEBUG: Environment.CurrentDirectory: {Environment.CurrentDirectory}");
                LogMessage($"DEBUG: AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                
                // Check if config files exist
                string cwdConfigPath = Path.Combine(Environment.CurrentDirectory, "config.json");
                string baseDirConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                LogMessage($"DEBUG: CWD config path: {cwdConfigPath} (exists: {File.Exists(cwdConfigPath)})");
                LogMessage($"DEBUG: BaseDir config path: {baseDirConfigPath} (exists: {File.Exists(baseDirConfigPath)})");
                
                // Try to load config and see what happens
                LogMessage("DEBUG: About to call Config.LoadFromFile()");
                
                // Let's manually check what ResolveConfigPath would return
                string manualCwdPath = Path.Combine(Environment.CurrentDirectory, "config.json");
                string manualBaseDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                LogMessage($"DEBUG: Manual CWD path: {manualCwdPath} (exists: {File.Exists(manualCwdPath)})");
                LogMessage($"DEBUG: Manual BaseDir path: {manualBaseDirPath} (exists: {File.Exists(manualBaseDirPath)})");
                
                // Try to read the file directly
                if (File.Exists(manualCwdPath))
                {
                    string directContent = File.ReadAllText(manualCwdPath);
                    LogMessage($"DEBUG: Direct read from CWD: {directContent}");
                }
                if (File.Exists(manualBaseDirPath))
                {
                    string directContent = File.ReadAllText(manualBaseDirPath);
                    LogMessage($"DEBUG: Direct read from BaseDir: {directContent}");
                }
                
                var reloaded = Config.LoadFromFile();
                LogMessage($"DEBUG: Loaded config - UseManual={reloaded.UseManualCapture}, AutoDetect={reloaded.AutoDetectCaptchaArea}, UseAbs={reloaded.UseAbsoluteCoordinates}, UseRel={reloaded.UseRelativeCoordinates}");
                LogMessage($"DEBUG: Config file path: {reloaded.LoadedPath}");
                LogMessage($"DEBUG: Config JSON deserialized values: UseManualCapture={reloaded.UseManualCapture}, UseAbsoluteCoordinates={reloaded.UseAbsoluteCoordinates}, UseRelativeCoordinates={reloaded.UseRelativeCoordinates}");
                
                // If LoadedPath is null, let's try to manually deserialize the JSON we read earlier
                if (string.IsNullOrEmpty(reloaded.LoadedPath))
                {
                    LogMessage("DEBUG: LoadedPath is null, trying manual deserialization...");
                    try
                    {
                        string manualJson = File.ReadAllText(manualCwdPath);
                        var manualConfig = JsonConvert.DeserializeObject<Config>(manualJson);
                        LogMessage($"DEBUG: Manual deserialization result - UseManualCapture={manualConfig?.UseManualCapture}, UseAbsoluteCoordinates={manualConfig?.UseAbsoluteCoordinates}, UseRelativeCoordinates={manualConfig?.UseRelativeCoordinates}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"ERROR: Manual deserialization failed: {ex.Message}");
                    }
                }
                
                // Check if the loaded path is different from what we expect
                if (string.IsNullOrEmpty(reloaded.LoadedPath))
                {
                    LogMessage("ERROR: Config.LoadedPath is null or empty!");
                    LogMessage("DEBUG: This means config was created with default values, not loaded from file!");
                }
                else
                {
                    LogMessage($"DEBUG: Config was loaded from: {reloaded.LoadedPath}");
                    
                    // Verify the file content
                    if (File.Exists(reloaded.LoadedPath))
                    {
                        string fileContent = File.ReadAllText(reloaded.LoadedPath);
                        LogMessage($"DEBUG: File content from {reloaded.LoadedPath}: {fileContent}");
                    }
                    else
                    {
                        LogMessage($"ERROR: Config.LoadedPath points to non-existent file: {reloaded.LoadedPath}");
                    }
                }
                // Respect the config file settings - don't override them
                if (reloaded.UseAbsoluteCoordinates)
                {
                    LogMessage("Config: Using absolute coordinates from config.json.");
                }
                else if (reloaded.AutoDetectCaptchaArea)
                {
                    LogMessage("Config: AutoDetectCaptchaArea enabled on Start.");
                }
                else
                {
                    LogMessage("Config: Using static coordinates from config (AutoDetectCaptchaArea disabled).");
                }
                _config = reloaded;
                _manualCapture = new ManualCaptchaCapture(_config);
                LogMessage($"Config flags: Manual={_config.UseManualCapture}, Abs={_config.UseAbsoluteCoordinates}, Rel={_config.UseRelativeCoordinates}");
                LogMessage($"Config values: UseManualCapture={_config.UseManualCapture}, UseAbsoluteCoordinates={_config.UseAbsoluteCoordinates}, UseRelativeCoordinates={_config.UseRelativeCoordinates}");
                // Masked key logging
                var key = _config.OCRSettings?.CapSolverAPIKey ?? string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    LogMessage($"CapSolver API key: {MaskKey(key)}");
                }
                // Update UI but suppress checkbox events to prevent config override
                _suppressManualEvents = true;
                UpdateUIForWindowState();
                _suppressManualEvents = false;
            }
            catch (Exception ex)
            {
                LogMessage($"Warning: Failed to reload config, using in-memory config. {ex.Message}");
            }

            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                LogMessage("Error: Please select a valid game window first.");
                ShowMessage("Please select a valid game window before starting.", "No Game Window", MessageBoxIcon.Warning);
                return;
            }

            // Validate manual coordinates before proceeding to avoid capturing (0,0) or clicking invalid points
            if (_config.UseManualCapture)
            {
                if (_manualCapture == null || !_manualCapture.IsValid())
                {
                    LogMessage("Error: Manual captcha area is invalid. Please set a valid area (>10x10).");
                    ShowMessage("Manual captcha area is invalid. Please set a valid area (>10x10).", "Invalid Area", MessageBoxIcon.Warning);
                    return;
                }

                if ((_manualCapture.InputField.X == 0 && _manualCapture.InputField.Y == 0) ||
                    (_manualCapture.ConfirmButton.X == 0 && _manualCapture.ConfirmButton.Y == 0))
                {
                    LogMessage("Error: Manual input/confirm points are not set. Please set both points.");
                    ShowMessage("Manual input/confirm points are not set. Please set both points.", "Missing Points", MessageBoxIcon.Warning);
                    return;
                }
            }

            // Set monitoring flag early to prevent race conditions
            _isMonitoring = true;

            try
            {
                // Update UI on UI thread immediately
                _btnStart.Enabled = false;
                _btnStartMonitoring.Enabled = false;
                _btnStop.Enabled = true;
                _btnStopMonitoring.Enabled = true;
                _lblStatus.Text = "Status: Initializing...";
                _lblStatus.ForeColor = WarningOrange;

                LogMessage("One-shot: Capturing captcha area...");

                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    try
                    {
                        int attempts = Math.Max(1, _config.AutomationSettings?.MaxRetries ?? 1);
                        bool success = false;
                        for (int i = 1; i <= attempts; i++)
                        {
                            LogMessage($"Attempt {i}/{attempts}: capturing and solving captcha...");
                            var bmp = await Task.Run(() => CaptureCaptchaAreaBitmap(), timeoutCts.Token);
                            if (bmp == null)
                            {
                                LogMessage("No captcha image captured.");
                            }
                            else
                            {
                                using (bmp)
                                {
                                    // Display captured captcha image in UI
                                    await this.InvokeAsync(() => {
                                        _picCaptcha.Image?.Dispose();
                                        _picCaptcha.Image = new Bitmap(bmp);
                                        _lblImageInfo.Text = $"Captcha: {bmp.Width}x{bmp.Height} | Area: {_lastCapturedArea}";
                                        _lblImageInfo.ForeColor = SuccessGreen;
                                        LogMessage($"🖼️ Captcha image displayed in UI: {bmp.Width}x{bmp.Height}");
                                    });
                                    
                                    string? text = SolveCaptchaWithOpenCVAndTesseract(bmp);
                                    if (!string.IsNullOrEmpty(text))
                                    {
                                        _lastCaptchaText = text;
                                        LogMessage($"Captcha solved: '{text}'");
                                        await TypeAndConfirmAsync(text, timeoutCts.Token);
                                        _successCount++;
                                        LogMessage("Processing result: Success");
                                        success = true;
                                        break;
                                    }
                                    else
                                    {
                                        _failureCount++;
                                        LogMessage("Processing result: Failure (empty OCR)");
                                    }
                                }
                            }
                            if (i < attempts)
                            {
                                await Task.Delay(_config.AutomationSettings?.DelayBetweenAttempts ?? 1000, timeoutCts.Token);
                            }
                        }
                        if (!success)
                        {
                            LogMessage("All attempts failed.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogMessage("One-shot: Operation timed out.");
                    }
                }

                LogMessage("One-shot operation completed.");
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Status: Error";
                _lblStatus.ForeColor = DangerRed;
                LogMessage($"Failed to start monitoring: {ex.Message}");
            }
            finally
            {
                // Always reset UI state
                _isMonitoring = false;
                _btnStart.Enabled = true;
                _btnStartMonitoring.Enabled = true;
                _btnStop.Enabled = false;
                _btnStopMonitoring.Enabled = false;
                _lblStatus.Text = "Status: Idle";
                _lblStatus.ForeColor = SecondaryBlue;
                _lblMonitoringStatus.Text = "Monitoring: Stopped";
                _lblMonitoringStatus.ForeColor = SecondaryBlue;
            }
        }

        private async void BtnStopMonitoring_Click(object? sender, EventArgs e)
        {
            if (!_isMonitoring)
            {
                LogMessage("Monitoring is not running.");
                return;
            }

            await this.InvokeAsync(() => {
                _lblStatus.Text = "Status: Stopping...";
                _lblStatus.ForeColor = WarningOrange;
            });

            try
            {
                // one-shot: nothing to cancel
            }
            catch (Exception ex)
            {
                LogMessage($"Exception while stopping monitoring task: {ex.Message}");
            }
            finally
            {
                _monitoringCts?.Dispose();
                _monitoringCts = null;
                _monitoringTask = null;
                _isMonitoring = false;
                _watchdogTimer.Stop();

                await this.InvokeAsync(() => {
                    _btnStart.Enabled = true;
                    _btnStartMonitoring.Enabled = true;
                    _btnStop.Enabled = false;
                    _btnStopMonitoring.Enabled = false;
                    _lblStatus.Text = "Status: Stopped";
                    _lblStatus.ForeColor = SecondaryBlue;
                    _lblMonitoringStatus.Text = "Monitoring: Stopped";
                    _lblMonitoringStatus.ForeColor = SecondaryBlue;
                    LogMessage("Monitoring stopped successfully.");
                });
            }
        }

        private async Task MonitoringLoop(CancellationToken token)
        {
            LogMessage("Monitoring loop started on background thread.");
            var stopwatch = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                stopwatch.Restart();
                try
                {
                    if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
                    {
                        await this.InvokeAsync(() => LogMessage("Game window became invalid. Stopping."));
                        break; 
                    }

                    // simplified mode: loop disabled
                    
                    stopwatch.Stop();
                    LogMessage($"Monitoring cycle took {stopwatch.ElapsedMilliseconds}ms.");

                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    LogMessage("Monitoring loop cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    await this.InvokeAsync(() => LogMessage($"Error in monitoring loop: {ex.Message}"));
                    await Task.Delay(2000, token); 
                }
            }
            await this.InvokeAsync(() => {
                LogMessage("Monitoring loop exited.");
            });
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                ShowMessage("Please select a valid game window first.", "Error", MessageBoxIcon.Error);
                return;
            }
            LogMessage("Testing OCR (OpenCV + Tesseract)...");
            try
            {
                var roi = GetEffectiveCaptchaArea();
                if (roi.Width <= 0 || roi.Height <= 0)
                {
                    LogMessage("❌ Test failed: ROI width/height <= 0");
                    ShowMessage($"ROI invalid: {roi}", "Test OCR", MessageBoxIcon.Warning);
                    return;
                }

                string summary = string.Empty;
                await Task.Run(() =>
                {
                    var bmp = CaptureCaptchaAreaBitmap();
                    if (bmp == null)
                    {
                        LogMessage("❌ Test failed: cannot capture captcha.");
                        summary = $"ROI={roi} | capture failed";
                        return;
                    }
                    using (bmp)
                    {
                        // Display captured captcha image in UI for test
                        this.Invoke(() => {
                            _picCaptcha.Image?.Dispose();
                            _picCaptcha.Image = new Bitmap(bmp);
                            _lblImageInfo.Text = $"Test Captcha: {bmp.Width}x{bmp.Height} | Area: {roi}";
                            _lblImageInfo.ForeColor = SuccessGreen;
                            LogMessage($"🖼️ Test captcha image displayed in UI: {bmp.Width}x{bmp.Height}");
                        });
                        
                        string text = SolveCaptchaWithOpenCVAndTesseract(bmp) ?? string.Empty;
                        if (!string.IsNullOrEmpty(text))
                        {
                            _lastCaptchaText = text;
                            LogMessage($"✅ Test successful: '{text}' via {_lastOcrMethod}");
                            summary = $"ROI={roi} | Image={bmp.Width}x{bmp.Height} | Method={_lastOcrMethod} | Result='{text}'";
                        }
                        else
                        {
                            LogMessage("❌ Test failed: empty result");
                            summary = $"ROI={roi} | Image={bmp.Width}x{bmp.Height} | Result=empty";
                        }
                    }
                });
                if (!string.IsNullOrEmpty(summary))
                {
                    ShowMessage(summary, "OpenCV + Tesseract Test Result", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Test error: {ex.Message}");
            }
        }

        private void BtnDebug_Click(object? sender, EventArgs e) { try { LogMessage("== Game Windows =="); LogMessage(WindowFinder.FindGameWindows()); LogMessage("== All Windows =="); LogMessage(WindowFinder.FindAllWindows()); } catch (Exception ex) { LogMessage($"Debug error: {ex.Message}"); } }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            _lblStats.Text = $"Success: {_successCount} | Fail: {_failureCount}";
            if (!string.IsNullOrEmpty(_lastCaptchaText))
            {
                _lblCaptcha.Text = $"Captcha: {_lastCaptchaText}";
            }
            _lblPerformanceStats.Text = "Performance: N/A";
        }

        // Service events removed

        private void LogMessage(string message) 
        { 
            if (this.InvokeRequired) 
            { 
                try 
                {
                    this.BeginInvoke(new Action(() => LogMessage(message)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error invoking LogMessage: {ex.Message}");
                }
                return; 
            } 
            if (_txtLog == null) 
            { 
                Console.WriteLine($"[LOG] {message}"); 
                return; 
            } 
            try 
            { 
                string timestamp = DateTime.Now.ToString("HH:mm:ss"); 
                _txtLog.AppendText($"[{timestamp}] {message}\n"); 
                _txtLog.SelectionStart = _txtLog.Text.Length;
                _txtLog.ScrollToCaret();
            } 
            catch (Exception ex) 
            { 
                Console.WriteLine($"Error in LogMessage: {ex.Message}"); 
            } 
        }

        // Monitoring status mapping removed

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isMonitoring)
            {
                BtnStopMonitoring_Click(this, EventArgs.Empty);
            }
            _watchdogTimer?.Dispose();
            _statusTimer?.Dispose();
            _previewTimer?.Dispose();
            try { _tessEngine?.Dispose(); } catch { }
            base.OnFormClosing(e);
        }

        private void BtnCaptureDebug_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Starting debug capture...");
                if (_selectedGameWindow == null)
                {
                    using (var selector = new GameWindowSelector())
                    {
                        if (selector.ShowDialog() == DialogResult.OK)
                        {
                            _selectedGameWindow = selector.SelectedWindow;
                        }
                        else
                        {
                    LogMessage("Debug capture canceled.");
                    return;
                        }
                    }
                }
                if (_selectedGameWindow == null) return;
                var bmp = CaptureCaptchaAreaBitmap();
                if (bmp == null)
                {
                    LogMessage("Debug capture: no image");
                    return;
                }
                Directory.CreateDirectory("debug_captures");
                string path = Path.Combine("debug_captures", $"capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                bmp.Save(path);
                bmp.Dispose();
                DisplayImage(path, "Debug Capture");
                LogMessage("Debug capture saved and displayed.");
            }
            catch (Exception ex)
            {
                LogMessage($"Debug capture error: {ex.Message}");
            }
        }

        private void DisplayImage(string imagePath, string description) { try { if (File.Exists(imagePath)) { using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read)) { using (var image = Image.FromStream(fs)) { _picCaptcha.Image?.Dispose(); _picCaptcha.Image = new Bitmap(image); } } _lblImageInfo.Text = $"Image: {description} ({Path.GetFileName(imagePath)})"; _lblImageInfo.ForeColor = Color.Green; } } catch (Exception ex) { LogMessage($"Error displaying image: {ex.Message}"); } }

        private void PicCaptcha_Click(object? sender, EventArgs e) { if (_picCaptcha.Image != null) { try { var imageViewer = new Form { Text = "Image Viewer", Size = new Size(800, 600), StartPosition = FormStartPosition.CenterParent }; Image imageCopy = new Bitmap(_picCaptcha.Image); var picFull = new PictureBox { Image = imageCopy, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill }; imageViewer.FormClosed += (s, args) => { picFull.Image?.Dispose(); }; imageViewer.Controls.Add(picFull); imageViewer.ShowDialog(); } catch (Exception ex) { Console.WriteLine($"Error displaying full image: {ex.Message}"); } } }

        private async Task AutoDetectGameWindow()
        {
            try
            {
                LogMessage("Auto-detecting game window...");
                await Task.Delay(500);
                var gameWindow = new GameWindow("Duke Client");
                if (gameWindow.FindGameWindowWithMultipleInstances())
                {
                    _selectedGameWindow = gameWindow;
                    await this.InvokeAsync(() =>
                    {
                        _lblSelectedWindow.Text = $"Window: {gameWindow.WindowTitle}";
                        _lblSelectedWindow.ForeColor = SuccessGreen;
                        LogMessage($"Auto-detected window: {gameWindow.WindowTitle}");
                        StartGameWindowPreview();
                        _suppressManualEvents = true;
                        UpdateUIForWindowState();
                        _suppressManualEvents = false;
                    });
                }
                else
                {
                    LogMessage("No game window found.");
                }
            }
            catch (Exception ex) { LogMessage($"Auto-detect error: {ex.Message}"); }
        }

        private async void BtnTestOpenCV_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("🧪 Testing OpenCV + Tesseract solver...");
                
                if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
                {
                    ShowMessage("Please select a valid game window first.", "Error", MessageBoxIcon.Error);
                    return;
                }

                string testResult = await Task.Run(() =>
                {
                    var bmp = CaptureCaptchaAreaBitmap();
                    if (bmp == null)
                    {
                        return "❌ OpenCV test: cannot capture image";
                    }
                    using (bmp)
                    {
                        string text = SolveCaptchaWithOpenCVAndTesseract(bmp) ?? string.Empty;
                        return $"🧪 Test Result: '{text}'";
                    }
                });
                LogMessage(testResult);
                ShowMessage(testResult, "OpenCV + Tesseract Test Result", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string errorMsg = $"❌ OpenCV test error: {ex.Message}";
                LogMessage(errorMsg);
                ShowMessage(errorMsg, "Test Error", MessageBoxIcon.Error);
            }
        }

        private void UpdateUIForWindowState()
        {
            bool windowIsValid = _selectedGameWindow != null && _selectedGameWindow.IsValid();
            _btnStart.Enabled = windowIsValid;
            _btnStartMonitoring.Enabled = windowIsValid;
            _btnTest.Enabled = windowIsValid;
            _btnCaptureDebug.Enabled = windowIsValid;

            // Enable/disable manual controls based on config
            bool manual = _config?.UseManualCapture ?? false;
            if (_btnSetCaptchaArea != null) _btnSetCaptchaArea.Enabled = manual;
            if (_btnSetInputField != null) _btnSetInputField.Enabled = manual;
            if (_btnSetConfirmButton != null) _btnSetConfirmButton.Enabled = manual;
            if (_chkUseManual != null)
            {
                // Only update checkbox if we're not already suppressing events
                if (!_suppressManualEvents)
                {
                    _suppressManualEvents = true;
                    _chkUseManual.Checked = manual;
                    _suppressManualEvents = false;
                }
                else
                {
                    // If already suppressing, just set the value directly
                    _chkUseManual.Checked = manual;
                }
            }

            if (!windowIsValid)
            {
                _lblSelectedWindow.Text = "Game Window: Not Selected";
                _lblSelectedWindow.ForeColor = DangerRed;
                StopGameWindowPreview();
            }
        }

        private void Watchdog_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_isMonitoring) return;

            bool hangDetected = false;
            if (_monitoringTask != null && (_monitoringTask.IsFaulted || _monitoringTask.IsCanceled))
            {
                LogMessage("[Watchdog] Monitoring task is faulted or cancelled.");
                hangDetected = true;
            }

            // Only evaluate preview staleness if we have a valid last update timestamp
            if (_lastPreviewUpdateTime != DateTime.MinValue &&
                DateTime.UtcNow - _lastPreviewUpdateTime > TimeSpan.FromSeconds(10))
            {
                LogMessage("[Watchdog] Preview has not updated in 10 seconds.");
                hangDetected = true;
            }

            if (hangDetected)
            {
                this.InvokeAsync(() => {
                    LogMessage("[Watchdog] Hang detected! Forcing stop.");
                    BtnStopMonitoring_Click(this, EventArgs.Empty);
                });
            }
        }

        private void BtnSetCaptchaArea_Click(object? sender, EventArgs e)
        {
            if (!_config.UseManualCapture)
            {
                LogMessage("Manual mode is off. Enable 'Use Manual' to set coordinates.");
                ShowMessage("Manual mode is off.", "Info", MessageBoxIcon.Information);
                return;
            }
            if (_picGamePreview.Image == null)
            {
                LogMessage("Cannot set area: Game preview is not active.");
                ShowMessage("Game preview is not active.", "Info", MessageBoxIcon.Information);
                return;
            }
            _selectionMode = SelectionMode.Area;
            _isSelecting = false; // Will be set to true on MouseDown
            LogMessage("MODE: Set Captcha Area. Please drag a rectangle on the game preview window.");
            _picGamePreview.Cursor = Cursors.Cross;
        }

        private void BtnSetInputField_Click(object? sender, EventArgs e)
        {
            if (!_config.UseManualCapture)
            {
                LogMessage("Manual mode is off. Enable 'Use Manual' to set coordinates.");
                ShowMessage("Manual mode is off.", "Info", MessageBoxIcon.Information);
                return;
            }
            if (_picGamePreview.Image == null)
            {
                LogMessage("Cannot set point: Game preview is not active.");
                ShowMessage("Game preview is not active.", "Info", MessageBoxIcon.Information);
                return;
            }
            _selectionMode = SelectionMode.InputField;
            _isSelecting = false;
            LogMessage("MODE: Set Input Field. Please click the text input location on the game preview window.");
            _picGamePreview.Cursor = Cursors.Cross;
        }

        private void BtnSetConfirmButton_Click(object? sender, EventArgs e)
        {
            if (!_config.UseManualCapture)
            {
                LogMessage("Manual mode is off. Enable 'Use Manual' to set coordinates.");
                ShowMessage("Manual mode is off.", "Info", MessageBoxIcon.Information);
                return;
            }
            if (_picGamePreview.Image == null)
            {
                LogMessage("Cannot set point: Game preview is not active.");
                ShowMessage("Game preview is not active.", "Info", MessageBoxIcon.Information);
                return;
            }
            _selectionMode = SelectionMode.ConfirmButton;
            _isSelecting = false;
            LogMessage("MODE: Set Confirm Button. Please click the confirm button location on the game preview window.");
            _picGamePreview.Cursor = Cursors.Cross;
        }

        private void BtnDebugROI_Click(object? sender, EventArgs e)
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                LogMessage("Error: Please select a valid game window first.");
                ShowMessage("Please select a valid game window first.", "Error", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                LogMessage("🔍 Debug ROI: Getting effective captcha area...");
                _selectedGameWindow.UpdateBounds();
                var area = GetEffectiveCaptchaArea();
                
                if (area.Width <= 0 || area.Height <= 0)
                {
                    LogMessage("❌ Debug ROI: Invalid area detected");
                    ShowMessage($"Invalid ROI area: {area}", "Debug ROI", MessageBoxIcon.Warning);
                    return;
                }

                LogMessage($"🔍 Debug ROI: Drawing rectangle at {area.X},{area.Y} {area.Width}x{area.Height}");
                DrawROIRectangle(area, "Debug ROI");
                
                // Also show padded version
                int padX = Math.Max(2, area.Width / 8);
                int padY = Math.Max(2, area.Height / 6);
                var padded = new Rectangle(area.X - padX, area.Y - padY, area.Width + padX * 2, area.Height + padY * 2);
                
                // Clamp to screen bounds
                var screenBounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                padded = Rectangle.Intersect(padded, screenBounds);
                
                LogMessage($"🔍 Debug ROI: Drawing padded rectangle at {padded.X},{padded.Y} {padded.Width}x{padded.Height}");
                DrawROIRectangle(padded, "Padded ROI");
            }
            catch (Exception ex)
            {
                LogMessage($"Debug ROI error: {ex.Message}");
                ShowMessage($"Debug ROI error: {ex.Message}", "Error", MessageBoxIcon.Error);
            }
        }

        private void BtnTestROI_Click(object? sender, EventArgs e)
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                LogMessage("Error: Please select a valid game window first.");
                ShowMessage("Please select a valid game window first.", "Error", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                LogMessage("🧪 Test ROI: Capturing and analyzing image...");
                _selectedGameWindow.UpdateBounds();
                var area = GetEffectiveCaptchaArea();
                
                if (area.Width <= 0 || area.Height <= 0)
                {
                    LogMessage("❌ Test ROI: Invalid area detected");
                    ShowMessage($"Invalid ROI area: {area}", "Test ROI", MessageBoxIcon.Warning);
                    return;
                }

                // Draw ROI rectangle first
                DrawROIRectangle(area, "Test ROI");
                
                // Capture image
                var image = CaptureCaptchaAreaBitmap();
                if (image == null)
                {
                    LogMessage("❌ Test ROI: Failed to capture image");
                    ShowMessage("Failed to capture image", "Test ROI", MessageBoxIcon.Error);
                    return;
                }

                using (image)
                {
                    // Display in UI
                    _picCaptcha.Image?.Dispose();
                    _picCaptcha.Image = new Bitmap(image);
                    _lblImageInfo.Text = $"Test ROI: {image.Width}x{image.Height} | Area: {area}";
                    _lblImageInfo.ForeColor = SuccessGreen;
                    
                    LogMessage($"✅ Test ROI: Image captured and displayed in UI");
                    ShowMessage($"ROI Test Complete\nArea: {area}\nImage: {image.Width}x{image.Height}\nCheck the preview to see if this area contains captcha text.", "Test ROI", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Test ROI error: {ex.Message}");
                ShowMessage($"Test ROI error: {ex.Message}", "Error", MessageBoxIcon.Error);
            }
        }

        private void BtnDebugFullWindow_Click(object? sender, EventArgs e)
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                LogMessage("Error: Please select a valid game window first.");
                ShowMessage("Please select a valid game window first.", "Error", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                LogMessage("🔍 Debug Full Window: Capturing full window with coordinate grid...");
                _selectedGameWindow.UpdateBounds();
                var bounds = _selectedGameWindow.Bounds;
                
                LogMessage($"Window bounds: {bounds.X},{bounds.Y} {bounds.Width}x{bounds.Height}");
                
                // Capture full window
                var fullImage = ScreenCapture.CaptureScreen(bounds);
                if (fullImage == null)
                {
                    LogMessage("❌ Debug Full Window: Failed to capture full window");
                    ShowMessage("Failed to capture full window", "Debug Full Window", MessageBoxIcon.Error);
                    return;
                }

                using (fullImage)
                {
                    // Create grid overlay
                    var gridImage = DrawCoordinateGrid(fullImage, bounds);
                    
                    // Save both images
                    Directory.CreateDirectory("coordinate_debug");
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    
                    // Save original
                    string originalPath = Path.Combine("coordinate_debug", $"full_window_original_{timestamp}.png");
                    fullImage.Save(originalPath);
                    
                    // Save with grid
                    string gridPath = Path.Combine("coordinate_debug", $"full_window_grid_{timestamp}.png");
                    gridImage.Save(gridPath);
                    
                    // Also capture current ROI area
                    var roiArea = GetEffectiveCaptchaArea();
                    if (roiArea.Width > 0 && roiArea.Height > 0)
                    {
                        // Convert client coordinates to screen coordinates
                        var screenRoi = new Rectangle(
                            bounds.X + roiArea.X,
                            bounds.Y + roiArea.Y,
                            roiArea.Width,
                            roiArea.Height
                        );
                        
                        var roiImage = ScreenCapture.CaptureScreen(screenRoi);
                        if (roiImage != null)
                        {
                            using (roiImage)
                            {
                                string roiPath = Path.Combine("coordinate_debug", $"current_captcha_crop_{timestamp}.png");
                                roiImage.Save(roiPath);
                                LogMessage($"💾 Saved ROI crop: {roiPath}");
                            }
                        }
                    }
                    
                    LogMessage($"💾 Saved full window debug images:");
                    LogMessage($"   Original: {originalPath}");
                    LogMessage($"   With Grid: {gridPath}");
                    LogMessage($"   ROI Area: {roiArea}");
                    
                    // Display in UI
                    _picCaptcha.Image?.Dispose();
                    _picCaptcha.Image = new Bitmap(gridImage);
                    _lblImageInfo.Text = $"Debug Full: {bounds.Width}x{bounds.Height} | ROI: {roiArea}";
                    _lblImageInfo.ForeColor = PrimaryBlue;
                    
                    ShowMessage($"Debug Full Window Complete\nWindow: {bounds.Width}x{bounds.Height}\nROI: {roiArea}\nImages saved to coordinate_debug folder", "Debug Full Window", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Debug Full Window error: {ex.Message}");
                ShowMessage($"Debug Full Window error: {ex.Message}", "Error", MessageBoxIcon.Error);
            }
        }

        private void PicGamePreview_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_selectionMode == SelectionMode.None) return;

            _selectionStart = e.Location;
            if (_selectionMode == SelectionMode.Area)
            {
                _isSelecting = true;
                _selectionRect = new Rectangle(e.Location, new Size(0, 0));
            }
            _picGamePreview.Invalidate();
        }

        private void PicGamePreview_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_selectionMode != SelectionMode.Area || !_isSelecting || e.Button != MouseButtons.Left) return;
            
            int x = Math.Min(_selectionStart.X, e.X);
            int y = Math.Min(_selectionStart.Y, e.Y);
            int w = Math.Abs(e.X - _selectionStart.X);
            int h = Math.Abs(e.Y - _selectionStart.Y);
            _selectionRect = new Rectangle(x, y, w, h);
            _picGamePreview.Invalidate();
        }

        private void PicGamePreview_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_selectionMode == SelectionMode.None) return;

            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                LogMessage("Error: Cannot set coordinates without a valid game window selected.");
                ResetSelectionMode();
                return;
            }

            Point windowScreenLocation = _selectedGameWindow.Bounds.Location;

            switch (_selectionMode)
            {
                case SelectionMode.Area:
                    _isSelecting = false;
                    if (_selectionRect.Width < 10 || _selectionRect.Height < 10)
                    {
                        LogMessage("Selection cancelled: Area is too small.");
                        break;
                    }

                    int clientX = (int)((_selectionRect.X - _lastPreviewOffsetX) / _lastPreviewScale);
                    int clientY = (int)((_selectionRect.Y - _lastPreviewOffsetY) / _lastPreviewScale);
                    int clientW = (int)(_selectionRect.Width / _lastPreviewScale);
                    int clientH = (int)(_selectionRect.Height / _lastPreviewScale);

                    Rectangle screenRect = new Rectangle(
                        windowScreenLocation.X + clientX,
                        windowScreenLocation.Y + clientY,
                        clientW,
                        clientH
                    );

                    _manualCapture.SetCaptchaArea(screenRect);
                    LogMessage($"✅ Manual Captcha Area set to: {screenRect} (Screen Coords)");
                    break;

                case SelectionMode.InputField:
                case SelectionMode.ConfirmButton:
                    int pointClientX = (int)((e.X - _lastPreviewOffsetX) / _lastPreviewScale);
                    int pointClientY = (int)((e.Y - _lastPreviewOffsetY) / _lastPreviewScale);

                    Point screenPoint = new Point(
                        windowScreenLocation.X + pointClientX,
                        windowScreenLocation.Y + pointClientY
                    );

                    if (_selectionMode == SelectionMode.InputField)
                    {
                        _manualCapture.SetInputField(screenPoint);
                        LogMessage($"✅ Manual Input Field set to: {screenPoint} (Screen Coords)");
                    }
                    else
                    {
                        _manualCapture.SetConfirmButton(screenPoint);
                        LogMessage($"✅ Manual Confirm Button set to: {screenPoint} (Screen Coords)");
                    }
                    break;
            }

            _config.SaveToFile();
            LogMessage("Configuration saved.");
            ResetSelectionMode();
            _picGamePreview.Invalidate();
        }

        private void ResetSelectionMode()
        {
            _selectionMode = SelectionMode.None;
            _isSelecting = false;
            _selectionRect = Rectangle.Empty;
            _picGamePreview.Cursor = Cursors.Default;
        }

        private void PicGamePreview_Paint(object? sender, PaintEventArgs e)
        {
            if (_isSelecting && _selectionMode == SelectionMode.Area && _selectionRect.Width > 0)
            {
                using var selectionPen = new Pen(Color.Cyan, 2);
                e.Graphics.DrawRectangle(selectionPen, _selectionRect);
                using var selectionBrush = new SolidBrush(Color.FromArgb(80, Color.Cyan));
                e.Graphics.FillRectangle(selectionBrush, _selectionRect);
            }

            if (_config == null || _selectedGameWindow == null || !_selectedGameWindow.IsValid()) return;

            if (_config.UseManualCapture)
            {
                Point windowScreenLocation = _selectedGameWindow.Bounds.Location;

                if (_manualCapture.IsValid())
                {
                    Rectangle screenArea = _manualCapture.CaptchaArea;
                    int clientX = screenArea.X - windowScreenLocation.X;
                    int clientY = screenArea.Y - windowScreenLocation.Y;

                    int previewX = (int)(clientX * _lastPreviewScale) + _lastPreviewOffsetX;
                    int previewY = (int)(clientY * _lastPreviewScale) + _lastPreviewOffsetY;
                    int previewW = (int)(screenArea.Width * _lastPreviewScale);
                    int previewH = (int)(screenArea.Height * _lastPreviewScale);
                    
                    Rectangle previewRect = new Rectangle(previewX, previewY, previewW, previewH);

                    using var areaPen = new Pen(Color.Red, 2);
                    e.Graphics.DrawRectangle(areaPen, previewRect);
                    e.Graphics.DrawString("Captcha", new Font("Arial", 8), Brushes.White, previewRect.Location);
                }

                Point inputPoint = _manualCapture.InputField;
                if (inputPoint.X > 0)
                {
                    int clientX = inputPoint.X - windowScreenLocation.X;
                    int clientY = inputPoint.Y - windowScreenLocation.Y;
                    int previewX = (int)(clientX * _lastPreviewScale) + _lastPreviewOffsetX;
                    int previewY = (int)(clientY * _lastPreviewScale) + _lastPreviewOffsetY;
                    Rectangle pointRect = new Rectangle(previewX - 4, previewY - 4, 8, 8);
                    using var inputPen = new Pen(Color.LawnGreen, 2);
                    e.Graphics.DrawEllipse(inputPen, pointRect);
                    e.Graphics.DrawString("Input", new Font("Arial", 8), Brushes.LawnGreen, new Point(previewX + 5, previewY - 15));
                }

                Point confirmPoint = _manualCapture.ConfirmButton;
                if (confirmPoint.X > 0)
                {
                    int clientX = confirmPoint.X - windowScreenLocation.X;
                    int clientY = confirmPoint.Y - windowScreenLocation.Y;
                    int previewX = (int)(clientX * _lastPreviewScale) + _lastPreviewOffsetX;
                    int previewY = (int)(clientY * _lastPreviewScale) + _lastPreviewOffsetY;
                    Rectangle pointRect = new Rectangle(previewX - 4, previewY - 4, 8, 8);
                    using var confirmPen = new Pen(Color.Yellow, 2);
                    e.Graphics.DrawEllipse(confirmPen, pointRect);
                    e.Graphics.DrawString("Confirm", new Font("Arial", 8), Brushes.Yellow, new Point(previewX + 5, previewY - 15));
                }
            }
        }

        private Size GetClientSize(IntPtr hWnd)
        {
            try
            {
                RECT clientRect;
                if (GetClientRect(hWnd, out clientRect))
                {
                    return new Size(clientRect.Right - clientRect.Left, clientRect.Bottom - clientRect.Top);
                }
                return Size.Empty;
            }
            catch
            {
                return Size.Empty;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private Rectangle GetEffectiveCaptchaArea()
        {
            var cfg = _config; // Use in-memory config
            LogMessage($"DEBUG: GetEffectiveCaptchaArea - UseManual={cfg.UseManualCapture}, AutoDetect={cfg.AutoDetectCaptchaArea}, UseAbs={cfg.UseAbsoluteCoordinates}, UseRel={cfg.UseRelativeCoordinates}");

            // Priority 0: Manual Capture (screen coords)
            if (cfg.UseManualCapture)
            {
                LogMessage("DEBUG: Checking manual capture...");
                if (_manualCapture != null && _manualCapture.IsValid())
                {
                    _lastRoiMode = "manual";
                    LogMessage($"ROI method: manual (screen) area={_manualCapture.CaptchaArea}");
                    return _manualCapture.CaptchaArea;
                }
                LogMessage("⚠️ Manual capture enabled but area is not valid. Falling back to other methods.");
            }

            // Priority 1: Advanced Auto-detect at runtime (client coords)
            if (cfg.AutoDetectCaptchaArea && _selectedGameWindow != null && _selectedGameWindow.IsValid())
            {
                LogMessage("DEBUG: Checking auto-detect...");
                try
                {
                    var detected = TryAdvancedAutoDetectCaptchaArea();
                    if (detected.Width > 0 && detected.Height > 0)
                    {
                        _lastRoiMode = "auto-detect";
                        LogMessage($"ROI method: auto-detect (client) area={detected}");
                        return detected;
                    }
                    else
                    {
                        LogMessage("🔎 Advanced auto-detect failed, falling back to config coordinates.");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Advanced auto-detect error: {ex.Message}. Falling back to config coordinates.");
                }
            }

            // Priority 2: Absolute screen coordinates
            if (cfg.UseAbsoluteCoordinates)
            {
                LogMessage("DEBUG: Using absolute coordinates...");
                var abs = new Rectangle(
                    cfg.CaptchaLeftX,
                    cfg.CaptchaTopY,
                    cfg.CaptchaRightX - cfg.CaptchaLeftX,
                    cfg.CaptchaBottomY - cfg.CaptchaTopY);
                _lastRoiMode = "absolute";
                LogMessage($"ROI method: absolute (screen) area={abs}");
                return abs;
            }

            // Priority 3: Relative coordinates based on client size
            if (cfg.UseRelativeCoordinates && _selectedGameWindow != null && _selectedGameWindow.IsValid())
            {
                var clientSize = GetClientSize(_selectedGameWindow.Handle);
                int x = Math.Max(0, (int)Math.Round(cfg.CaptchaAreaRelative.X * clientSize.Width));
                int y = Math.Max(0, (int)Math.Round(cfg.CaptchaAreaRelative.Y * clientSize.Height));
                int w = Math.Max(1, (int)Math.Round(cfg.CaptchaAreaRelative.Width * clientSize.Width));
                int h = Math.Max(1, (int)Math.Round(cfg.CaptchaAreaRelative.Height * clientSize.Height));
                var rel = new Rectangle(x, y, w, h);
                _lastRoiMode = "relative";
                LogMessage($"ROI method: relative (client) area={rel}");
                return rel;
            }

            // Priority 4: Fixed client coordinates
            _lastRoiMode = "fixed";
            LogMessage($"ROI method: fixed (client) area={cfg.CaptchaArea}");
            return cfg.CaptchaArea;
        }

        // Advanced auto-detection using multiple OpenCV methods
        private Rectangle TryAdvancedAutoDetectCaptchaArea()
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid()) return Rectangle.Empty;

            try
            {
                // Capture full client area
                var clientSize = GetClientSize(_selectedGameWindow.Handle);
                if (clientSize.Width < 10 || clientSize.Height < 10) return Rectangle.Empty;

                using var fullScreenshot = ScreenCapture.CaptureWindowClientArea(_selectedGameWindow.Handle, 
                    new Rectangle(0, 0, clientSize.Width, clientSize.Height));
                if (fullScreenshot == null) return Rectangle.Empty;

                // Convert to OpenCV Mat
                using var ms = new MemoryStream();
                fullScreenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = ms.ToArray();
                using var matScreenshot = Cv2.ImDecode(bytes, ImreadModes.Color);
                if (matScreenshot.Empty()) return Rectangle.Empty;

                // Use advanced ROI detector
                var detector = new AutoCaptchaROIDetector(enableDebugOutput: true);
                var result = detector.DetectCaptchaRegion(matScreenshot);

                if (result.Success)
                {
                    LogMessage($"🎯 ROI Detection: {result.Method}, Confidence: {result.Confidence:F1}%, Time: {result.ProcessingTime.TotalMilliseconds:F0}ms");
                    LogMessage($"📊 Debug: {result.DebugInfo}");
                    
                    // Save debug image with detected region
                    detector.SaveDebugImage(matScreenshot, result.Region, result.Method);
                    
                    return result.Region;
                }
                else
                {
                    LogMessage($"❌ ROI Detection failed: {result.DebugInfo}");
                    return Rectangle.Empty;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Advanced auto-detect exception: {ex.Message}");
                return Rectangle.Empty;
            }
        }

        private Bitmap? CaptureCaptchaAreaBitmap()
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid()) return null;
            var cfg = _config;
            LogMessage($"Loaded config: UseManual={cfg.UseManualCapture}, UseAbs={cfg.UseAbsoluteCoordinates}");
            _selectedGameWindow.UpdateBounds();
            var area = GetEffectiveCaptchaArea();
            
            // Draw ROI rectangle on screen for debugging before capture
            DrawROIRectangle(area, "Detected ROI");
            
            // Slightly pad the detected ROI to avoid cutting off glyph edges
            try
            {
                int padX = Math.Max(2, area.Width / 8);   // ~12.5%
                int padY = Math.Max(2, area.Height / 6);  // ~16%
                var padded = new Rectangle(area.X - padX, area.Y - padY, area.Width + padX * 2, area.Height + padY * 2);

                if (_config.UseManualCapture || _config.UseAbsoluteCoordinates)
                {
                    // Clamp to virtual screen for screen-space capture
                    var screenBounds = System.Windows.Forms.SystemInformation.VirtualScreen;
                    padded = Rectangle.Intersect(padded, screenBounds);
                    LogMessage($"ROI method: {(_config.UseManualCapture ? "manual" : (_config.UseAbsoluteCoordinates ? "absolute" : "auto-detect"))} (screen) area={area} -> padded={padded}");
                }
                else
                {
                    // Clamp to client rect for client capture
                    var cs = GetClientSize(_selectedGameWindow.Handle);
                    var clientRect = new Rectangle(0, 0, cs.Width, cs.Height);
                    padded = Rectangle.Intersect(padded, clientRect);
                    LogMessage($"ROI method: auto-detect (client) area={area} -> padded={padded}");
                }
                
                // Draw padded ROI rectangle
                DrawROIRectangle(padded, "Padded ROI");
                area = padded;
            }
            catch { }
            
            try
            {
                // NEW WORKFLOW: Capture full window first, then crop from it
                Bitmap? fullWindowImage = null;
                Bitmap? croppedImage = null;
                
                // Step 1: Capture full game window
                if (cfg.UseManualCapture || cfg.UseAbsoluteCoordinates)
                {
                    // For screen coordinates, capture the full screen area around the game window
                    var windowBounds = _selectedGameWindow.Bounds;
                    fullWindowImage = ScreenCapture.CaptureScreen(windowBounds);
                    LogMessage($"📸 Captured full screen area: {windowBounds}");
                }
                else
                {
                    // For client coordinates, capture the full client area
                    var clientSize = GetClientSize(_selectedGameWindow.Handle);
                    var clientRect = new Rectangle(0, 0, clientSize.Width, clientSize.Height);
                    fullWindowImage = ScreenCapture.CaptureWindowClientArea(_selectedGameWindow.Handle, clientRect);
                    LogMessage($"📸 Captured full client area: {clientRect}");
                }
                
                if (fullWindowImage == null)
                {
                    LogMessage("❌ Failed to capture full window image");
                    return null;
                }
                
                // Step 2: Draw coordinate grid on full window image
                var imageWithGrid = DrawCoordinateGridOnImage(fullWindowImage, _selectedGameWindow.Bounds);
                
                // Step 3: Crop the captcha area from the image with grid
                if (cfg.UseManualCapture || cfg.UseAbsoluteCoordinates)
                {
                    // For screen coordinates, crop from screen position
                    var windowBounds = _selectedGameWindow.Bounds;
                    var screenArea = new Rectangle(area.X - windowBounds.X, area.Y - windowBounds.Y, area.Width, area.Height);
                    croppedImage = CropImage(imageWithGrid, screenArea);
                    LogMessage($"✂️ Cropped from screen coordinates: {screenArea}");
                }
                else
                {
                    // For client coordinates, crop directly from client area
                    croppedImage = CropImage(imageWithGrid, area);
                    LogMessage($"✂️ Cropped from client coordinates: {area}");
                }
                
                // Clean up full window image
                fullWindowImage.Dispose();
                imageWithGrid.Dispose();
                
                if (croppedImage != null)
                {
                    Directory.CreateDirectory("captcha_debug");
                    string filename = $"captcha_enhanced_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                    string relativePath = Path.Combine("captcha_debug", filename);
                    string fullPath = Path.GetFullPath(relativePath);
                    croppedImage.Save(fullPath);
                    _lastCaptchaImagePath = fullPath;
                    _lastCapturedArea = area;
                    LogMessage($"📐 Captured area: X={area.X}, Y={area.Y}, W={area.Width}, H={area.Height}");
                    LogMessage($"💾 Saved captcha image: {_lastCaptchaImagePath}");
                    
                    // Debug: Show image info
                    LogMessage($"🔍 Image analysis: Size={croppedImage.Width}x{croppedImage.Height}, PixelFormat={croppedImage.PixelFormat}");
                    
                    // Check if image is mostly empty (all same color)
                    using (var bmp = new Bitmap(croppedImage))
                    {
                        var colors = new Dictionary<Color, int>();
                        for (int x = 0; x < Math.Min(10, bmp.Width); x += 2)
                        {
                            for (int y = 0; y < Math.Min(10, bmp.Height); y += 2)
                            {
                                var color = bmp.GetPixel(x, y);
                                colors[color] = colors.GetValueOrDefault(color, 0) + 1;
                            }
                        }
                        var dominantColor = colors.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
                        LogMessage($"🎨 Dominant color: {dominantColor.Key} (appears {dominantColor.Value} times)");
                        
                        if (colors.Count <= 2)
                        {
                            LogMessage("⚠️ WARNING: Image appears to be mostly solid color - may not contain captcha text!");
                        }
                    }
                }
                return croppedImage;
            }
            catch (Exception ex)
            {
                LogMessage($"Capture error: {ex.Message}");
                return null;
            }
        }

        private string? SolveCaptchaWithOpenCVAndTesseract(Bitmap bmp)
        {
            try
            {
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = ms.ToArray();
                using var matColor = Cv2.ImDecode(bytes, ImreadModes.Color);
                if (matColor.Empty()) return string.Empty;

                // Enhanced preprocessing for colored captcha
                using var matGray = new Mat();
                Cv2.CvtColor(matColor, matGray, ColorConversionCodes.BGR2GRAY);

                // Multiple preprocessing approaches
                var results = new List<(string Method, string Text)>();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                
                // Approach 1: Standard Otsu threshold
                using var matBinary1 = new Mat();
                Cv2.Threshold(matGray, matBinary1, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("Otsu", TryOCR(matBinary1, "Otsu")));

                // Approach 1b: Inverted Otsu (light text on dark background)
                using var matBinary1b = new Mat();
                Cv2.Threshold(matGray, matBinary1b, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                results.Add(("OtsuInv", TryOCR(matBinary1b, "OtsuInv")));

                // Approach 2: Adaptive threshold (Mean)
                using var matBinary2 = new Mat();
                Cv2.AdaptiveThreshold(matGray, matBinary2, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 11, 2);
                results.Add(("AdaptiveMean", TryOCR(matBinary2, "AdaptiveMean")));

                // Approach 3: Morphological operations to clean noise
                using var matBinary3 = new Mat();
                Cv2.Threshold(matGray, matBinary3, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
                Cv2.MorphologyEx(matBinary3, matBinary3, MorphTypes.Close, kernel);
                Cv2.MorphologyEx(matBinary3, matBinary3, MorphTypes.Open, kernel);
                results.Add(("Morphology", TryOCR(matBinary3, "Morphology")));

                // Approach 4: Gaussian blur + threshold for smooth text
                using var matBlur = new Mat();
                Cv2.GaussianBlur(matGray, matBlur, new OpenCvSharp.Size(3, 3), 0);
                using var matBinary4 = new Mat();
                Cv2.Threshold(matBlur, matBinary4, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("Blur+Otsu", TryOCR(matBinary4, "Blur+Otsu")));

                // Approach 5: Upscale x3 + CLAHE + Otsu (improves small text)
                using var matUpscaled = new Mat();
                Cv2.Resize(matGray, matUpscaled, new OpenCvSharp.Size(), 3.0, 3.0, InterpolationFlags.Cubic);
                using var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new OpenCvSharp.Size(8, 8));
                using var matClahe = new Mat();
                clahe.Apply(matUpscaled, matClahe);
                using var matBinary5 = new Mat();
                Cv2.Threshold(matClahe, matBinary5, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("Upscale3x+CLAHE+Otsu", TryOCR(matBinary5, "Upscale3x+CLAHE+Otsu")));

                // Approach 5b: Upscale x4 + sharpen + OtsuInv
                using var matUpscaled4 = new Mat();
                Cv2.Resize(matGray, matUpscaled4, new OpenCvSharp.Size(), 4.0, 4.0, InterpolationFlags.Cubic);
                using var sharpenKernel = new Mat(3, 3, MatType.CV_32F, new float[] { 0,-1,0, -1,5,-1, 0,-1,0 });
                using var matSharpen = new Mat();
                Cv2.Filter2D(matUpscaled4, matSharpen, matUpscaled4.Depth(), sharpenKernel);
                using var matBinary5b = new Mat();
                Cv2.Threshold(matSharpen, matBinary5b, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                results.Add(("Upscale4x+Sharpen+OtsuInv", TryOCR(matBinary5b, "Upscale4x+Sharpen+OtsuInv")));

                // Approach 6: HSV magenta/pink isolation (common for Duke captcha)
                using var matHsv = new Mat();
                Cv2.CvtColor(matColor, matHsv, ColorConversionCodes.BGR2HSV);
                using var mask1 = new Mat();
                using var mask2 = new Mat();
                // Magenta ranges in HSV
                Cv2.InRange(matHsv, new Scalar(140, 60, 60), new Scalar(180, 255, 255), mask1); // high H
                Cv2.InRange(matHsv, new Scalar(0, 60, 60), new Scalar(15, 255, 255), mask2);    // wrap range (red-ish)
                using var matMask = new Mat();
                Cv2.BitwiseOr(mask1, mask2, matMask);
                using var matMaskClean = new Mat();
                var k2 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                Cv2.MorphologyEx(matMask, matMaskClean, MorphTypes.Open, k2);
                Cv2.MorphologyEx(matMaskClean, matMaskClean, MorphTypes.Close, k2);
                using var matBinary6 = new Mat();
                // invert so text is dark on light for Tesseract
                Cv2.BitwiseNot(matMaskClean, matBinary6);
                results.Add(("HSV-Magenta", TryOCR(matBinary6, "HSV-Magenta")));

                // Approach 7: Canny + dilate → fill → threshold
                using var canny = new Mat();
                Cv2.Canny(matGray, canny, 50, 150);
                using var cannyDilate = new Mat();
                var k3 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
                Cv2.Dilate(canny, cannyDilate, k3);
                using var matBinary7 = new Mat();
                Cv2.Threshold(cannyDilate, matBinary7, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("Canny+Dilate", TryOCR(matBinary7, "Canny+Dilate")));

                // Approach 8: Upscale 2x + bilateralFilter + adaptiveThreshold Gaussian
                using var matUpscale2 = new Mat();
                Cv2.Resize(matGray, matUpscale2, new OpenCvSharp.Size(), 2.0, 2.0, InterpolationFlags.Cubic);
                using var matBilateral = new Mat();
                Cv2.BilateralFilter(matUpscale2, matBilateral, 9, 75, 75);
                using var matBinary8 = new Mat();
                Cv2.AdaptiveThreshold(matBilateral, matBinary8, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                results.Add(("Upscale2x+Bilateral+AdaptiveGaussian", TryOCR(matBinary8, "Upscale2x+Bilateral+AdaptiveGaussian")));

                // Approach 9: Special handling for long captcha text (5x upscale + denoise)
                using var matUpscale5 = new Mat();
                Cv2.Resize(matGray, matUpscale5, new OpenCvSharp.Size(), 5.0, 5.0, InterpolationFlags.Cubic);
                using var matDenoise = new Mat();
                Cv2.FastNlMeansDenoising(matUpscale5, matDenoise);
                using var matBinary9 = new Mat();
                Cv2.Threshold(matDenoise, matBinary9, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("Upscale5x+Denoise+Otsu", TryOCR(matBinary9, "Upscale5x+Denoise+Otsu")));

                // Approach 10: Color-based segmentation for orange/brown background
                using var matBinary10 = new Mat();
                using var matHSV10 = new Mat();
                Cv2.CvtColor(matColor, matHSV10, ColorConversionCodes.BGR2HSV);
                using var mask = new Mat();
                // Create mask for non-orange/brown areas (text areas)
                Cv2.InRange(matHSV10, new Scalar(0, 0, 0), new Scalar(30, 255, 200), mask);
                Cv2.BitwiseNot(mask, mask);
                Cv2.BitwiseAnd(matGray, matGray, matBinary10, mask);
                Cv2.Threshold(matBinary10, matBinary10, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("ColorMask+Otsu", TryOCR(matBinary10, "ColorMask+Otsu")));

                // Approach 11: Edge detection + dilation for text extraction
                using var matBinary11 = new Mat();
                using var edges = new Mat();
                Cv2.Canny(matGray, edges, 50, 150);
                using var kernel11 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3));
                Cv2.Dilate(edges, matBinary11, kernel11);
                Cv2.Threshold(matBinary11, matBinary11, 0, 255, ThresholdTypes.Binary);
                results.Add(("EdgeDilate+Binary", TryOCR(matBinary11, "EdgeDilate+Binary")));

                // Approach 12: High contrast enhancement
                using var matBinary12 = new Mat();
                using var matEnhanced = new Mat();
                Cv2.ConvertScaleAbs(matGray, matEnhanced, 2.0, -100); // Increase contrast
                Cv2.Threshold(matEnhanced, matBinary12, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("HighContrast+Otsu", TryOCR(matBinary12, "HighContrast+Otsu")));

                // Approach 13: Orange background removal using color range
                using var matBinary13 = new Mat();
                using var matLab = new Mat();
                Cv2.CvtColor(matColor, matLab, ColorConversionCodes.BGR2Lab);
                using var orangeMask = new Mat();
                // Define orange color range in Lab space
                Cv2.InRange(matLab, new Scalar(0, 100, 100), new Scalar(255, 150, 150), orangeMask);
                Cv2.BitwiseNot(orangeMask, orangeMask);
                Cv2.BitwiseAnd(matGray, matGray, matBinary13, orangeMask);
                Cv2.Threshold(matBinary13, matBinary13, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("OrangeMask+Otsu", TryOCR(matBinary13, "OrangeMask+Otsu")));

                // Approach 14: Color-based text extraction (for colored CAPTCHA)
                using var matBinary14 = new Mat();
                using var matHSV14 = new Mat();
                Cv2.CvtColor(matColor, matHSV14, ColorConversionCodes.BGR2HSV);
                using var textMask = new Mat();
                // Create mask for colored text (not white background)
                Cv2.InRange(matHSV14, new Scalar(0, 50, 50), new Scalar(180, 255, 255), textMask);
                Cv2.BitwiseAnd(matGray, matGray, matBinary14, textMask);
                Cv2.Threshold(matBinary14, matBinary14, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("ColorText+Otsu", TryOCR(matBinary14, "ColorText+Otsu")));

                // Approach 15: Multi-color channel processing
                using var matBinary15 = new Mat();
                using var matBGR = new Mat();
                Cv2.CvtColor(matColor, matBGR, ColorConversionCodes.BGR2RGB);
                var channels15 = new Mat[3];
                Cv2.Split(matBGR, out channels15);
                using var combined15 = new Mat();
                Cv2.BitwiseOr(channels15[0], channels15[1], combined15);
                Cv2.BitwiseOr(combined15, channels15[2], combined15);
                Cv2.Threshold(combined15, matBinary15, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("MultiChannel+Otsu", TryOCR(matBinary15, "MultiChannel+Otsu")));
                // Dispose channels manually
                foreach (var channel in channels15)
                {
                    channel?.Dispose();
                }

                // Approach 16: Edge detection on color channels
                using var matBinary16 = new Mat();
                using var matEdges = new Mat();
                Cv2.Canny(matGray, matEdges, 30, 100);
                using var kernel16 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2));
                Cv2.Dilate(matEdges, matBinary16, kernel16);
                Cv2.Threshold(matBinary16, matBinary16, 0, 255, ThresholdTypes.Binary);
                results.Add(("EdgeColor+Binary", TryOCR(matBinary16, "EdgeColor+Binary")));

                // Approach 17: Color channel separation and combination
                using var matBinary17 = new Mat();
                var channels17 = new Mat[3];
                Cv2.Split(matColor, out channels17);
                using var blue = channels17[0];
                using var green = channels17[1];
                using var red = channels17[2];
                using var combined17 = new Mat();
                Cv2.BitwiseOr(blue, green, combined17);
                Cv2.BitwiseOr(combined17, red, combined17);
                Cv2.Threshold(combined17, matBinary17, 50, 255, ThresholdTypes.Binary);
                results.Add(("ColorSeparation+Binary", TryOCR(matBinary17, "ColorSeparation+Binary")));
                // Dispose channels manually
                foreach (var channel in channels17)
                {
                    channel?.Dispose();
                }

                // Approach 18: HSV-based text extraction with multiple thresholds
                using var matBinary18 = new Mat();
                using var matHSV18 = new Mat();
                Cv2.CvtColor(matColor, matHSV18, ColorConversionCodes.BGR2HSV);
                using var mask1_18 = new Mat();
                using var mask2_18 = new Mat();
                using var mask3_18 = new Mat();
                // Red range
                Cv2.InRange(matHSV18, new Scalar(0, 50, 50), new Scalar(10, 255, 255), mask1_18);
                // Green range
                Cv2.InRange(matHSV18, new Scalar(40, 50, 50), new Scalar(80, 255, 255), mask2_18);
                // Blue range
                Cv2.InRange(matHSV18, new Scalar(100, 50, 50), new Scalar(130, 255, 255), mask3_18);
                using var combinedMask18 = new Mat();
                Cv2.BitwiseOr(mask1_18, mask2_18, combinedMask18);
                Cv2.BitwiseOr(combinedMask18, mask3_18, combinedMask18);
                Cv2.BitwiseAnd(matGray, matGray, matBinary18, combinedMask18);
                Cv2.Threshold(matBinary18, matBinary18, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                results.Add(("HSVMultiColor+Otsu", TryOCR(matBinary18, "HSVMultiColor+Otsu")));

                // Choose best result: prefer length 3–8, then any 2–10, with more flexible regex
                var rx = new Regex("^[a-zA-Z0-9]{2,10}$");
                var valid = results.Where(r => !string.IsNullOrEmpty(r.Text) && rx.IsMatch(r.Text)).ToList();
                var preferred = valid.Where(r => r.Text.Length >= 3 && r.Text.Length <= 8).ToList();
                
                // If no valid results, try more lenient validation
                if (valid.Count == 0)
                {
                    var lenientRx = new Regex("^[a-zA-Z0-9]{1,15}$");
                    var lenientValid = results.Where(r => !string.IsNullOrEmpty(r.Text) && lenientRx.IsMatch(r.Text)).ToList();
                    if (lenientValid.Count > 0)
                    {
                        LogMessage($"Using lenient validation: found {lenientValid.Count} results");
                        valid = lenientValid;
                        preferred = lenientValid.Where(r => r.Text.Length >= 2 && r.Text.Length <= 10).ToList();
                    }
                }
                (string Method, string Text)? chosen = null;
                if (preferred.Count > 0)
                {
                    chosen = preferred.OrderByDescending(r => r.Text.Length).First();
                }
                else if (valid.Count > 0)
                {
                    chosen = valid.OrderByDescending(r => r.Text.Length).First();
                }

                if (chosen.HasValue)
                {
                    _lastOcrMethod = chosen.Value.Method;
                    LogMessage($"OCR Best result: '{chosen.Value.Text}' via {_lastOcrMethod} from {valid.Count} valid results");
                    return chosen.Value.Text;
                }

                LogMessage($"OCR failed: All {results.Count} approaches returned empty/invalid results");
                _lastOcrMethod = string.Empty;
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogMessage($"OCR error: {ex.Message}");
                _lastOcrMethod = string.Empty;
                return string.Empty;
            }
        }

        private string TryOCR(Mat binaryMat, string method)
        {
            Mat processedMat = binaryMat;
            try
            {
                LogMessage($"OCR {method}: Input size {binaryMat.Width}x{binaryMat.Height}");
                
                // Resize if too small (Tesseract works better with larger images)
                if (binaryMat.Width < 200 || binaryMat.Height < 50)
                {
                    var scale = Math.Max(200.0 / binaryMat.Width, 50.0 / binaryMat.Height);
                    var newWidth = (int)(binaryMat.Width * scale);
                    var newHeight = (int)(binaryMat.Height * scale);
                    
                    processedMat = new Mat();
                    Cv2.Resize(binaryMat, processedMat, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Nearest);
                    LogMessage($"OCR {method}: Resized to {newWidth}x{newHeight} (scale={scale:F2})");
                }

                // Save debug image for each method
                if (_enableDebugOutput)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    processedMat.SaveImage(Path.Combine("captcha_debug", $"ocr_{method}_{timestamp}.png"));
                }

                using var binBmp = processedMat.ToBitmap();
                using var ms2 = new MemoryStream();
                binBmp.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                var pix = Pix.LoadFromMemory(ms2.ToArray());
                using (pix)
                {
                    var engine = _tessEngine;
                    if (engine == null) 
                    {
                        LogMessage($"OCR {method}: Tesseract engine is null");
                        return string.Empty;
                    }
                    
                    using var page = engine.Process(pix);
                    var text = page.GetText().Trim();
                    var originalText = text;
                    text = new string(text.Where(char.IsLetterOrDigit).ToArray());
                    
                    LogMessage($"OCR {method}: Raw='{originalText}', Filtered='{text}', Length={text.Length}");
                    
                    // More detailed logging for debugging
                    if (string.IsNullOrEmpty(text))
                    {
                        LogMessage($"OCR {method}: FAILED - empty result after filtering");
                        return string.Empty;
                    }
                    
                    // Flexible regex filter
                    if (!Regex.IsMatch(text, "^[a-zA-Z0-9]{2,10}$"))
                    {
                        LogMessage($"OCR {method}: FAILED - regex mismatch (text: '{text}', length: {text.Length})");
                        return string.Empty;
                    }

                    LogMessage($"OCR {method}: SUCCESS '{text}'");
                    return text;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"OCR {method} error: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                // Dispose processedMat if it was created
                if (processedMat != binaryMat)
                {
                    processedMat?.Dispose();
                }
            }
        }

        private async Task TypeAndConfirmAsync(string captchaText, CancellationToken token)
        {
            var cfg = _config;
            Point inputPointClient;
            Point confirmPointClient;
            string mode;

            if (cfg.UseManualCapture && _manualCapture != null)
            {
                mode = "manual";
                // Manual points are stored as screen coordinates; convert to client coords
                var inputScreen = _manualCapture.InputField;
                var confirmScreen = _manualCapture.ConfirmButton;
                if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
                {
                    LogMessage("ERROR: Cannot perform input, game window is not valid.");
                    return;
                }
                Point clientOrigin = new Point(0, 0);
                ClientToScreen(_selectedGameWindow.Handle, ref clientOrigin);
                inputPointClient = new Point(inputScreen.X - clientOrigin.X, inputScreen.Y - clientOrigin.Y);
                confirmPointClient = new Point(confirmScreen.X - clientOrigin.X, confirmScreen.Y - clientOrigin.Y);
            }
            else if (cfg.UseAbsoluteCoordinates)
            {
                mode = "absolute";
                // Treat absolute Input/Confirm points as client coordinates for consistency
                inputPointClient = new Point(cfg.InputFieldX, cfg.InputFieldY);
                confirmPointClient = new Point(cfg.ConfirmButtonX, cfg.ConfirmButtonY);
            }
            else
            {
                mode = cfg.UseRelativeCoordinates ? "relative" : "fixed";
                // Positions are client coordinates
                inputPointClient = cfg.InputFieldPosition;
                confirmPointClient = cfg.ConfirmButtonPosition;
            }

            LogMessage($"Automation points: Input=({inputPointClient.X},{inputPointClient.Y}) Confirm=({confirmPointClient.X},{confirmPointClient.Y}) mode={mode} (client-coords)");

            InputAutomation.ClickInWindow(_selectedGameWindow!.Handle, inputPointClient);
            await Task.Delay(cfg.AutomationSettings.DelayAfterClick, token);
            InputAutomation.SendTextToWindow(_selectedGameWindow!.Handle, captchaText);
            await Task.Delay(cfg.AutomationSettings.DelayAfterInput, token);

            InputAutomation.ClickInWindow(_selectedGameWindow!.Handle, confirmPointClient);
        }
        private void DrawROIRectangle(Rectangle rect, string label)
        {
            try
            {
                // Create a transparent overlay window to draw the rectangle
                var overlay = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Normal,
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(rect.X, rect.Y),
                    Size = new Size(rect.Width, rect.Height),
                    TopMost = true,
                    ShowInTaskbar = false,
                    BackColor = Color.Red,
                    Opacity = 0.3,
                    Text = label
                };

                // Add a label to show the rectangle info
                var lbl = new Label
                {
                    Text = $"{label}\n{rect.X},{rect.Y} {rect.Width}x{rect.Height}",
                    ForeColor = Color.White,
                    BackColor = Color.Red,
                    Font = new Font("Arial", 8, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                overlay.Controls.Add(lbl);

                // Show the overlay
                overlay.Show();
                LogMessage($"🔍 Drawing {label}: {rect.X},{rect.Y} {rect.Width}x{rect.Height}");

                // Auto-hide after 3 seconds
                var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += (s, e) => {
                    timer.Stop();
                    overlay.Close();
                    overlay.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                LogMessage($"Error drawing ROI rectangle: {ex.Message}");
            }
        }

        private Bitmap DrawCoordinateGrid(Bitmap originalImage, Rectangle windowBounds)
        {
            var gridImage = new Bitmap(originalImage);
            using (var g = Graphics.FromImage(gridImage))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Grid settings
                int gridSize = 50; // 50px grid
                var gridPen = new Pen(Color.Red, 1);
                var textBrush = new SolidBrush(Color.Red);
                var font = new Font("Arial", 8, FontStyle.Bold);
                
                // Draw vertical lines
                for (int x = 0; x < windowBounds.Width; x += gridSize)
                {
                    g.DrawLine(gridPen, x, 0, x, windowBounds.Height);
                    if (x > 0) // Don't draw 0,0
                    {
                        g.DrawString(x.ToString(), font, textBrush, x + 2, 2);
                    }
                }
                
                // Draw horizontal lines
                for (int y = 0; y < windowBounds.Height; y += gridSize)
                {
                    g.DrawLine(gridPen, 0, y, windowBounds.Width, y);
                    if (y > 0) // Don't draw 0,0
                    {
                        g.DrawString(y.ToString(), font, textBrush, 2, y + 2);
                    }
                }
                
                // Highlight ROI area if available
                var roiArea = GetEffectiveCaptchaArea();
                if (roiArea.Width > 0 && roiArea.Height > 0)
                {
                    var roiPen = new Pen(Color.Lime, 3);
                    g.DrawRectangle(roiPen, roiArea);
                    
                    // Add ROI label
                    var roiLabel = $"ROI: {roiArea.X},{roiArea.Y} {roiArea.Width}x{roiArea.Height}";
                    var roiLabelBrush = new SolidBrush(Color.Lime);
                    g.DrawString(roiLabel, font, roiLabelBrush, roiArea.X, roiArea.Y - 20);
                }
                
                // Add window info
                var infoText = $"Window: {windowBounds.X},{windowBounds.Y} {windowBounds.Width}x{windowBounds.Height}";
                var infoBrush = new SolidBrush(Color.Yellow);
                g.DrawString(infoText, font, infoBrush, 10, windowBounds.Height - 30);
            }
            return gridImage;
        }

        private string MaskKey(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                if (key.Length <= 8) return new string('*', key.Length);
                return key.Substring(0, 4) + new string('*', key.Length - 8) + key.Substring(key.Length - 4);
            }
            catch { return "****"; }
        }

        private Bitmap DrawCoordinateGridOnImage(Bitmap originalImage, Rectangle windowBounds)
        {
            var imageWithGrid = new Bitmap(originalImage);
            using (var g = Graphics.FromImage(imageWithGrid))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw coordinate grid
                var gridPen = new Pen(Color.Red, 1);
                var labelFont = new Font("Arial", 8, FontStyle.Bold);
                var labelBrush = new SolidBrush(Color.Red);
                
                // Draw vertical lines every 50 pixels
                for (int x = 0; x < imageWithGrid.Width; x += 50)
                {
                    g.DrawLine(gridPen, x, 0, x, imageWithGrid.Height);
                    if (x > 0) // Don't draw label at 0
                    {
                        g.DrawString(x.ToString(), labelFont, labelBrush, x + 2, 2);
                    }
                }
                
                // Draw horizontal lines every 50 pixels
                for (int y = 0; y < imageWithGrid.Height; y += 50)
                {
                    g.DrawLine(gridPen, 0, y, imageWithGrid.Width, y);
                    if (y > 0) // Don't draw label at 0
                    {
                        g.DrawString(y.ToString(), labelFont, labelBrush, 2, y + 2);
                    }
                }
                
                // Draw origin label
                g.DrawString("(0,0)", labelFont, labelBrush, 2, 2);
                
                gridPen.Dispose();
                labelFont.Dispose();
                labelBrush.Dispose();
            }
            return imageWithGrid;
        }

        private Bitmap CropImage(Bitmap sourceImage, Rectangle cropArea)
        {
            // Ensure crop area is within image bounds
            cropArea = Rectangle.Intersect(cropArea, new Rectangle(0, 0, sourceImage.Width, sourceImage.Height));
            
            if (cropArea.Width <= 0 || cropArea.Height <= 0)
            {
                return new Bitmap(1, 1); // Return minimal bitmap if crop area is invalid
            }
            
            var croppedImage = new Bitmap(cropArea.Width, cropArea.Height);
            using (var g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(sourceImage, new Rectangle(0, 0, cropArea.Width, cropArea.Height), cropArea, GraphicsUnit.Pixel);
            }
            return croppedImage;
        }
        private void ShowMessage(string message, string title, MessageBoxIcon icon)
        {
            try
            {
                if (_config?.SilentMode == true)
                {
                    LogMessage($"[Silent] {title}: {message}");
                    return;
                }
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            }
            catch
            {
                // fallback to log only
                LogMessage($"[Silent-Fallback] {title}: {message}");
            }
        }
    }
}
