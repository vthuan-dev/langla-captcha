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

namespace langla_duky
{
    public partial class MainForm : Form
    {
        private CaptchaAutomationService _automationService = null!;
        private Button _btnStart = null!;
        private Button _btnStop = null!;
        private Button _btnTest = null!;
        private Button _btnDebug = null!;
        private Button _btnSelectWindow = null!;
        private Button _btnStartMonitoring = null!;
        private Button _btnStopMonitoring = null!;
        private Label _lblStatus = null!;
        private Label _lblCaptcha = null!;
        private Label _lblStats = null!;
        private Label _lblSelectedWindow = null!;
        private Label _lblMonitoringStatus = null!;
        private Label _lblPerformanceStats = null!;
        private TextBox _txtLog = null!;
        private PictureBox _picCaptcha = null!;
        private PictureBox _picGamePreview = null!;
        private Label _lblImageInfo = null!;
        private System.Windows.Forms.Timer _statusTimer = null!;
        private System.Windows.Forms.Timer _previewTimer = null!;
        private GameWindow? _selectedGameWindow;

        public MainForm()
        {
            InitializeComponent();
            InitializeAutomationService();

            // Thêm tùy chọn test OCR khi khởi động
            var btnQuickTest = new Button
            {
                Text = "🚀 Quick OCR Test",
                Location = new Point(350, 180),
                Size = new Size(140, 30),
                BackColor = Color.LightPink,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnQuickTest.Click += (s, e) =>
            {
                LogMessage("🔍 Bắt đầu chạy Quick OCR Test...");
                Task.Run(() =>
                {
                    try
                    {
                        // Phương thức RunQuickTest bây giờ tự xử lý tất cả logic,
                        // bao gồm cả việc hiển thị MessageBox khi hoàn thành hoặc gặp lỗi.
                        QuickOCRTest.RunQuickTest();

                        // Ghi log khi tác vụ hoàn thành mà không bị crash.
                        this.Invoke(new Action(() => LogMessage("✅ Quick OCR Test đã kết thúc.")));
                    }
                    catch (Exception ex)
                    {
                        // Bắt các ngoại lệ không mong muốn từ Task.
                        this.Invoke(new Action(() =>
                        {
                            string errorMsg = $"Lỗi nghiêm trọng trong quá trình test: {ex.Message}";
                            LogMessage(errorMsg);
                            MessageBox.Show(errorMsg, "Lỗi nghiêm trọng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                    }
                });
            };
            this.Controls.Add(btnQuickTest);
            
            // Thêm Simple OCR Test button
            var btnSimpleTest = new Button
            {
                Text = "🔧 Simple OCR Test",
                Location = new Point(350, 220),
                Size = new Size(140, 30),
                BackColor = Color.LightBlue,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnSimpleTest.Click += (s, e) =>
            {
                try
                {
                    LogMessage("🔧 Đang chạy Simple OCR Test...");
                    
                    // Chạy SimpleOCRTest và capture output
                    Task.Run(() => 
                    {
                        try
                        {
                            var originalOut = Console.Out;
                            using (var sw = new StringWriter())
                            {
                                Console.SetOut(sw);
                                SimpleOCRTest.RunSimpleTest();
                                Console.SetOut(originalOut);
                                
                                // Log kết quả
                                string output = sw.ToString();
                                this.Invoke(new Action(() => 
                                {
                                    LogMessage("===== SIMPLE OCR TEST RESULTS =====");
                                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        LogMessage(line);
                                    }
                                    LogMessage("==================================");
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() => LogMessage($"❌ Lỗi trong SimpleOCRTest: {ex.Message}")));
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Lỗi Simple OCR Test: {ex.Message}");
                }
            };
            this.Controls.Add(btnSimpleTest);

            // Thêm nút Capture và Process Captcha theo workflow mới
            var btnCaptureProcess = new Button
            {
                Text = "📸 Capture & Process",
                Location = new Point(500, 180),
                Size = new Size(150, 30),
                BackColor = Color.LightCoral,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnCaptureProcess.Click += BtnCaptureProcess_Click;
            this.Controls.Add(btnCaptureProcess);
            
            // Thêm nút Test OCR Direct
            var btnTestDirect = new Button
            {
                Text = "🔬 Test Direct",
                Location = new Point(500, 220),
                Size = new Size(150, 30),
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnTestDirect.Click += (s, e) =>
            {
                try
                {
                    LogMessage("🔬 Đang chạy Test OCR Direct...");
                    
                    Task.Run(() => 
                    {
                        try
                        {
                            var originalOut = Console.Out;
                            using (var sw = new StringWriter())
                            {
                                Console.SetOut(sw);
                                TestOCRDirect.RunDirectTest();
                                Console.SetOut(originalOut);
                                
                                string output = sw.ToString();
                                this.Invoke(new Action(() => 
                                {
                                    LogMessage("===== DIRECT OCR TEST RESULTS =====");
                                    foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        LogMessage(line);
                                    }
                                    LogMessage("==================================");
                                    
                                    // Hiển thị kết quả
                                    string debugFolder = "ocr_debug_output";
                                    if (Directory.Exists(debugFolder))
                                    {
                                        var imageFiles = Directory.GetFiles(debugFolder, "direct_*.png");
                                        if (imageFiles.Length > 0)
                                        {
                                            DisplayImage(imageFiles[0], "Direct OCR Test Image");
                                        }
                                        
                                        string resultPath = Path.Combine(debugFolder, "direct_ocr_result.txt");
                                        if (File.Exists(resultPath))
                                        {
                                            string recognizedText = File.ReadAllText(resultPath);
                                            _lblCaptcha.Text = $"Captcha: {recognizedText}";
                                            _lblCaptcha.ForeColor = Color.Green;
                                            LogMessage($"🔤 Direct OCR Result: \"{recognizedText}\"");
                                        }
                                    }
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Invoke(new Action(() => LogMessage($"❌ Lỗi trong TestOCRDirect: {ex.Message}")));
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Lỗi Test OCR Direct: {ex.Message}");
                }
            };
            this.Controls.Add(btnTestDirect);
        }

        private void InitializeComponent()
        {
            this.Text = "Làng Lá Duke - Captcha Automation Tool";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;

            // Status label
            _lblStatus = new Label
            {
                Text = "Trạng thái: Chưa khởi tạo",
                Location = new Point(20, 20),
                Size = new Size(400, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(_lblStatus);

            // Selected window label
            _lblSelectedWindow = new Label
            {
                Text = "Cửa sổ game: Chưa chọn",
                Location = new Point(430, 20),
                Size = new Size(350, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Red
            };
            this.Controls.Add(_lblSelectedWindow);

            // Captcha display
            _lblCaptcha = new Label
            {
                Text = "Captcha: Chưa có",
                Location = new Point(20, 50),
                Size = new Size(200, 20),
                Font = new Font("Arial", 10)
            };
            this.Controls.Add(_lblCaptcha);

            // Statistics
            _lblStats = new Label
            {
                Text = "Thành công: 0 | Thất bại: 0",
                Location = new Point(20, 80),
                Size = new Size(200, 20),
                Font = new Font("Arial", 10)
            };
            this.Controls.Add(_lblStats);

            // Monitoring Status
            _lblMonitoringStatus = new Label
            {
                Text = "Monitoring: Stopped",
                Location = new Point(250, 80),
                Size = new Size(200, 20),
                Font = new Font("Arial", 10),
                ForeColor = Color.Blue
            };
            this.Controls.Add(_lblMonitoringStatus);

            // Performance Stats
            _lblPerformanceStats = new Label
            {
                Text = "Performance: N/A",
                Location = new Point(20, 100),
                Size = new Size(400, 20),
                Font = new Font("Arial", 9),
                ForeColor = Color.DarkGreen
            };
            this.Controls.Add(_lblPerformanceStats);

            // Row 1: Main control buttons
            _btnStart = new Button
            {
                Text = "Bắt đầu",
                Location = new Point(20, 140),
                Size = new Size(80, 30),
                BackColor = Color.LightGreen
            };
            _btnStart.Click += BtnStart_Click;
            this.Controls.Add(_btnStart);

            _btnStop = new Button
            {
                Text = "Dừng",
                Location = new Point(110, 140),
                Size = new Size(80, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            _btnStop.Click += BtnStop_Click;
            this.Controls.Add(_btnStop);

            _btnTest = new Button
            {
                Text = "Test OCR",
                Location = new Point(200, 140),
                Size = new Size(80, 30),
                BackColor = Color.LightBlue
            };
            _btnTest.Click += BtnTest_Click;
            this.Controls.Add(_btnTest);

            _btnDebug = new Button
            {
                Text = "Debug",
                Location = new Point(290, 140),
                Size = new Size(80, 30),
                BackColor = Color.LightYellow
            };
            _btnDebug.Click += BtnDebug_Click;
            this.Controls.Add(_btnDebug);

            var btnCoordFinder = new Button
            {
                Text = "Find Coords",
                Location = new Point(380, 140),
                Size = new Size(80, 30),
                BackColor = Color.LightCyan
            };
            btnCoordFinder.Click += (s, e) =>
            {
                var finder = new CoordinateFinder();
                finder.ShowDialog();
            };
            this.Controls.Add(btnCoordFinder);

            _btnSelectWindow = new Button
            {
                Text = "Chọn cửa sổ",
                Location = new Point(470, 140),
                Size = new Size(100, 30),
                BackColor = Color.LightGoldenrodYellow,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            _btnSelectWindow.Click += BtnSelectWindow_Click;
            this.Controls.Add(_btnSelectWindow);

            // Row 2: Monitoring and Debug buttons
            _btnStartMonitoring = new Button
            {
                Text = "🎯 Monitor",
                Location = new Point(20, 180),
                Size = new Size(100, 30),
                BackColor = Color.LightSeaGreen,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Enabled = true
            };
            _btnStartMonitoring.Click += BtnStartMonitoring_Click;
            this.Controls.Add(_btnStartMonitoring);

            _btnStopMonitoring = new Button
            {
                Text = "⏸️ Stop",
                Location = new Point(130, 180),
                Size = new Size(80, 30),
                BackColor = Color.LightSalmon,
                Font = new Font("Arial", 9, FontStyle.Bold),
                Enabled = false
            };
            _btnStopMonitoring.Click += BtnStopMonitoring_Click;
            this.Controls.Add(_btnStopMonitoring);

            var btnCaptureDebug = new Button
            {
                Text = "Capture Debug",
                Location = new Point(220, 180),
                Size = new Size(120, 30),
                BackColor = Color.Orange
            };
            btnCaptureDebug.Click += BtnCaptureDebug_Click;
            this.Controls.Add(btnCaptureDebug);

            // Image preview label
            _lblImageInfo = new Label
            {
                Text = "Hình ảnh capture: (chưa có)",
                Location = new Point(540, 20),
                Size = new Size(320, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            this.Controls.Add(_lblImageInfo);

            // Picture box for captured images
            _picCaptcha = new PictureBox
            {
                Location = new Point(540, 50),
                Size = new Size(320, 150),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };
            _picCaptcha.Click += PicCaptcha_Click; // Click to view full size
            this.Controls.Add(_picCaptcha);
            
            // Game window preview
            _picGamePreview = new PictureBox
            {
                Location = new Point(540, 220),
                Size = new Size(320, 240),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.LightGray
            };
            this.Controls.Add(_picGamePreview);
            
            // Preview timer
            _previewTimer = new System.Windows.Forms.Timer();
            _previewTimer.Interval = 500; // Update every 500ms
            _previewTimer.Tick += PreviewTimer_Tick;

            // Log textbox
            _txtLog = new TextBox
            {
                Location = new Point(20, 220),
                Size = new Size(500, 350),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(_txtLog);

            // Status timer
            _statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        private void InitializeAutomationService()
        {
            _automationService = new CaptchaAutomationService();
            _automationService.CaptchaDetected += OnCaptchaDetected;
            _automationService.CaptchaProcessed += OnCaptchaProcessed;
            _automationService.ErrorOccurred += OnErrorOccurred;

            // Subscribe to new monitoring events
            _automationService.MonitoringStatusChanged += OnMonitoringStatusChanged;
            _automationService.ResponseReceived += OnResponseReceived;

            LogMessage("✅ Enhanced Captcha Automation Tool initialized");
            LogMessage("🎯 New features: Continuous Monitoring, Response Verification, Enhanced Stats");
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
                        _lblSelectedWindow.Text = $"Cửa sổ game: {_selectedGameWindow.WindowTitle}";
                        _lblSelectedWindow.ForeColor = Color.Green;
                        LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle} ({_selectedGameWindow.Bounds.Width}x{_selectedGameWindow.Bounds.Height})");
                        
                        // Start real-time preview immediately
                        StartGameWindowPreview();
                        
                        // Capture và hiển thị ảnh ngay lập tức
                        UpdateGameWindowPreview();
                        
                        LogMessage($"✅ Đã bắt đầu preview real-time cho cửa sổ game");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi khi chọn cửa sổ: {ex.Message}");
            }
        }
        
        private void StartGameWindowPreview()
        {
            if (_selectedGameWindow != null && _selectedGameWindow.IsValid())
            {
                // Stop existing timer if running
                if (_previewTimer.Enabled)
                {
                    _previewTimer.Stop();
                }
                
                _previewTimer.Interval = 500; // Update every 0.5 seconds for smoother preview
                _previewTimer.Tick += PreviewTimer_Tick;
                _previewTimer.Start();
                
                Console.WriteLine("✅ Started game window preview timer (500ms interval)");
                LogMessage("✅ Đã bắt đầu preview real-time cửa sổ game");
                
                // Update immediately
                UpdateGameWindowPreview();
            }
            else
            {
                Console.WriteLine("❌ Cannot start preview: Invalid game window");
                LogMessage("❌ Không thể bắt đầu preview: Cửa sổ game không hợp lệ");
            }
        }
        
        private void StopGameWindowPreview()
        {
            _previewTimer.Stop();
            LogMessage("Đã dừng preview cửa sổ game");
        }
        
        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            UpdateGameWindowPreview();
        }
        
        private void UpdateGameWindowPreview()
        {
            if (_selectedGameWindow == null || !_selectedGameWindow.IsValid())
            {
                Console.WriteLine("❌ Cannot update preview: No valid game window selected");
                return;
            }
            
            try
            {
                // Capture the entire game window
                Rectangle captureArea = new Rectangle(0, 0, _selectedGameWindow.Bounds.Width, _selectedGameWindow.Bounds.Height);
                Console.WriteLine($"🖼️ Capturing preview: {captureArea.Width}x{captureArea.Height}");
                
                using (Bitmap? capture = ScreenCapture.CaptureWindow(_selectedGameWindow.Handle, captureArea))
                {
                    if (capture != null)
                    {
                        // Create a copy of the capture for display
                        if (_picGamePreview.Image != null)
                        {
                            _picGamePreview.Image.Dispose();
                            _picGamePreview.Image = null;
                        }
                        
                        _picGamePreview.Image = new Bitmap(capture);
                        
                        // Update image info label
                        _lblImageInfo.Text = $"Game Preview: {capture.Width}x{capture.Height} - {DateTime.Now:HH:mm:ss}";
                        
                        Console.WriteLine($"✅ Preview updated: {capture.Width}x{capture.Height}");
                        
                        // Force refresh the picture box
                        _picGamePreview.Refresh();
                    }
                    else
                    {
                        Console.WriteLine("❌ Failed to capture game window");
                        _lblImageInfo.Text = "Game Preview: Capture failed";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating preview: {ex.Message}");
                _lblImageInfo.Text = $"Game Preview: Error - {ex.Message}";
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

                // Kiểm tra xem đã chọn cửa sổ game chưa
                if (_selectedGameWindow == null)
                {
                    // Hiển thị hộp thoại chọn cửa sổ
                    using (var selector = new GameWindowSelector())
                    {
                        if (selector.ShowDialog() == DialogResult.OK)
                        {
                            _selectedGameWindow = selector.SelectedWindow;
                            _lblSelectedWindow.Text = $"Cửa sổ game: {_selectedGameWindow.WindowTitle}";
                            _lblSelectedWindow.ForeColor = Color.Green;
                            LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}");
                        }
                        else
                        {
                            // Người dùng đã hủy việc chọn cửa sổ
                            _lblStatus.Text = "Trạng thái: Chưa chọn cửa sổ game";
                            _lblStatus.ForeColor = Color.Red;
                            _btnStart.Enabled = true;
                            _btnStop.Enabled = false;
                            return;
                        }
                    }
                }

                if (!_automationService.Initialize(_selectedGameWindow))
                {
                    _lblStatus.Text = "Trạng thái: Lỗi khởi tạo";
                    _lblStatus.ForeColor = Color.Red;
                    _btnStart.Enabled = true;
                    _btnStop.Enabled = false;
                    return;
                }

                _lblStatus.Text = "Trạng thái: Đang chạy...";
                _lblStatus.ForeColor = Color.Green;

                // FIXED: Start monitoring service trước khi bắt đầu automation
                _automationService.StartMonitoring();
                
                // Start automation loop
                await Task.Run(async () =>
                {
                    while (_btnStop.Enabled)
                    {
                        try
                        {
                            await _automationService.ProcessCaptchaAsync();
                            await Task.Delay(2000); // Wait between attempts - có thể config từ file
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Lỗi trong vòng lặp: {ex.Message}");
                            await Task.Delay(5000); // Wait 5 seconds on error
                        }
                    }
                });
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
            _lblStatus.ForeColor = Color.Blue;
            LogMessage("Đã dừng automation");

            // FIXED: Stop monitoring service khi dừng automation
            _automationService.StopMonitoring();

            // Không reset cửa sổ đã chọn để có thể tiếp tục sử dụng
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Đang test OCR...");

                // Kiểm tra xem đã chọn cửa sổ game chưa
                if (_selectedGameWindow == null)
                {
                    // Hiển thị hộp thoại chọn cửa sổ
                    using (var selector = new GameWindowSelector())
                    {
                        if (selector.ShowDialog() == DialogResult.OK)
                        {
                            _selectedGameWindow = selector.SelectedWindow;
                            _lblSelectedWindow.Text = $"Cửa sổ game: {_selectedGameWindow.WindowTitle}";
                            _lblSelectedWindow.ForeColor = Color.Green;
                            LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}");
                        }
                        else
                        {
                            // Người dùng đã hủy việc chọn cửa sổ
                            LogMessage("Đã hủy chọn cửa sổ. Không thể test.");
                            return;
                        }
                    }
                }

                if (!_automationService.Initialize(_selectedGameWindow))
                {
                    LogMessage("Không thể khởi tạo để test");
                    return;
                }

                await _automationService.ProcessCaptchaAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"Lỗi test: {ex.Message}");
            }
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
            catch (Exception ex)
            {
                LogMessage($"Lỗi debug: {ex.Message}");
            }
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (_automationService != null)
            {
                // Basic stats
                _lblStats.Text = $"Thành công: {_automationService.SuccessCount} | Thất bại: {_automationService.FailureCount}";

                if (!string.IsNullOrEmpty(_automationService.LastCaptchaText))
                {
                    _lblCaptcha.Text = $"Captcha: {_automationService.LastCaptchaText}";
                }

                // Enhanced performance stats
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
            this.Invoke(new Action(() =>
            {
                // FIXED: Handle null/empty captchaText safely
                if (string.IsNullOrEmpty(captchaText))
                {
                    LogMessage("Captcha được phát hiện: (không có text)");
                }
                else
                {
                    LogMessage($"Captcha được phát hiện: {captchaText}");
                }
            }));
        }

        private void OnCaptchaProcessed(object? sender, string captchaText)
        {
            this.Invoke(new Action(() =>
            {
                // FIXED: Handle null/empty captchaText safely
                if (string.IsNullOrEmpty(captchaText))
                {
                    LogMessage("Captcha đã được xử lý: (không có text)");
                }
                else
                {
                    LogMessage($"Captcha đã được xử lý: {captchaText}");
                }
            }));
        }

        private void OnErrorOccurred(object? sender, string error)
        {
            this.Invoke(new Action(() =>
            {
                LogMessage($"Lỗi: {error}");
            }));
        }

        private void LogMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _txtLog.AppendText($"[{timestamp}] {message}\r\n");
            _txtLog.SelectionStart = _txtLog.Text.Length;
            _txtLog.ScrollToCaret();
        }

        // New monitoring button event handlers
        private void BtnStartMonitoring_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_selectedGameWindow == null)
                {
                    // Hiển thị hộp thoại chọn cửa sổ
                    using (var selector = new GameWindowSelector())
                    {
                        if (selector.ShowDialog() == DialogResult.OK)
                        {
                            _selectedGameWindow = selector.SelectedWindow;
                            _lblSelectedWindow.Text = $"Cửa sổ game: {_selectedGameWindow.WindowTitle}";
                            _lblSelectedWindow.ForeColor = Color.Green;
                        }
                        else
                        {
                            LogMessage("Cần chọn cửa sổ game trước khi start monitoring");
                            return;
                        }
                    }
                }

                if (!_automationService.Initialize(_selectedGameWindow))
                {
                    LogMessage("❌ Không thể khởi tạo automation service");
                    return;
                }

                bool success = _automationService.StartContinuousMonitoring();

                if (success)
                {
                    _btnStartMonitoring.Enabled = false;
                    _btnStopMonitoring.Enabled = true;
                    _lblStatus.Text = "Trạng thái: Monitoring - Đang giám sát captcha";
                    _lblStatus.ForeColor = Color.Green;
                    LogMessage("🎯 Started continuous monitoring mode");
                }
                else
                {
                    LogMessage("❌ Failed to start monitoring");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi start monitoring: {ex.Message}");
            }
        }

        private void BtnStopMonitoring_Click(object? sender, EventArgs e)
        {
            try
            {
                _automationService.StopContinuousMonitoring();

                _btnStartMonitoring.Enabled = true;
                _btnStopMonitoring.Enabled = false;
                _lblStatus.Text = "Trạng thái: Monitoring stopped";
                _lblStatus.ForeColor = Color.Blue;
                _lblMonitoringStatus.Text = "Monitoring: Stopped";
                _lblMonitoringStatus.ForeColor = Color.Blue;

                LogMessage("⏸️ Stopped continuous monitoring");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi stop monitoring: {ex.Message}");
            }
        }

        // New event handlers for monitoring events
        private void OnMonitoringStatusChanged(object? sender, string status)
        {
            this.Invoke(new Action(() =>
            {
                _lblMonitoringStatus.Text = $"Monitoring: {status}";
                _lblMonitoringStatus.ForeColor = status.Contains("❌") ? Color.Red :
                                               status.Contains("✅") ? Color.Green :
                                               status.Contains("🔍") ? Color.Orange : Color.Blue;
                LogMessage($"📊 {status}");
            }));
        }

        private void OnResponseReceived(object? sender, CaptchaResponseResult result)
        {
            this.Invoke(new Action(() =>
            {
                string statusText = result.IsSuccess ? "✅ SUCCESS" : $"❌ {result.Status}";
                Color statusColor = result.IsSuccess ? Color.Green : Color.Red;

                LogMessage($"📨 Response: {statusText} - {result.Message}");

                // Update UI elements based on response
                if (result.IsSuccess)
                {
                    _lblCaptcha.ForeColor = Color.Green;
                }
                else
                {
                    _lblCaptcha.ForeColor = Color.Red;
                }
            }));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _automationService?.StopContinuousMonitoring();
                _automationService?.Dispose();
                _statusTimer?.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during form closing: {ex.Message}");
            }
            base.OnFormClosing(e);
        }

        private void BtnCaptureDebug_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("📸 Bắt đầu capture debug...");

                // Kiểm tra xem đã chọn cửa sổ game chưa
                if (_selectedGameWindow == null)
                {
                    // Hiển thị hộp thoại chọn cửa sổ
                    using (var selector = new GameWindowSelector())
                    {
                        if (selector.ShowDialog() == DialogResult.OK)
                        {
                            _selectedGameWindow = selector.SelectedWindow;
                            _lblSelectedWindow.Text = $"Cửa sổ game: {_selectedGameWindow.WindowTitle}";
                            _lblSelectedWindow.ForeColor = Color.Green;
                            LogMessage($"Đã chọn cửa sổ: {_selectedGameWindow.WindowTitle}");
                        }
                        else
                        {
                            // Người dùng đã hủy việc chọn cửa sổ
                            LogMessage("Đã hủy chọn cửa sổ. Không thể capture debug.");
                            return;
                        }
                    }
                }

                // Khởi tạo service với cửa sổ đã chọn
                if (!_automationService.Initialize(_selectedGameWindow))
                {
                    LogMessage("Không thể khởi tạo để capture debug");
                    return;
                }

                _automationService.RunCaptureDebug();

                // Tìm và hiển thị hình ảnh mới nhất
                string debugFolder = "debug_captures";
                if (Directory.Exists(debugFolder))
                {
                    var files = Directory.GetFiles(debugFolder, "*.png").OrderByDescending(f => File.GetCreationTime(f)).ToArray();
                    if (files.Length > 0)
                    {
                        // ƯU TIÊN hiển thị FULL WINDOW trước để phân tích
                        var fullWindowFile = files.FirstOrDefault(f => f.Contains("full_window"));
                        if (fullWindowFile != null)
                        {
                            DisplayImage(fullWindowFile, "Toàn bộ cửa sổ game");
                            LogMessage($"🖼️ Hiển thị full window để phân tích vị trí captcha");
                        }
                        else
                        {
                            // Nếu không có full window, hiển thị hình đầu tiên
                            DisplayImage(files[0], "Hình ảnh đã capture");
                        }

                        LogMessage($"✅ Đã lưu {files.Length} file hình ảnh");
                        LogMessage($"📷 Kiểm tra hình ảnh để xác định vị trí captcha chính xác");
                        LogMessage($"🔍 Click vào hình ảnh để xem to hơn");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi capture debug: {ex.Message}");
            }
        }

        private void DisplayImage(string imagePath, string description)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        var image = Image.FromStream(fs);
                        
                        if (_picCaptcha.Image != null)
                        {
                            _picCaptcha.Image.Dispose();
                            _picCaptcha.Image = null;
                        }
                        
                        _picCaptcha.Image = new Bitmap(image); // Create a copy
                        _lblImageInfo.Text = $"Hình ảnh: {description} ({Path.GetFileName(imagePath)})";
                        _lblImageInfo.ForeColor = Color.Green;
                    }
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
            // Hiển thị hình ảnh full size trong cửa sổ mới
            if (_picCaptcha.Image != null)
            {
                try
                {
                    var imageViewer = new Form
                    {
                        Text = "Xem hình ảnh toàn màn hình",
                        Size = new Size(Math.Max(600, _picCaptcha.Image.Width + 50), Math.Max(400, _picCaptcha.Image.Height + 100)),
                        StartPosition = FormStartPosition.CenterParent
                    };

                    // Create a copy of the image to avoid disposal issues
                    Image imageCopy = new Bitmap(_picCaptcha.Image);
                    
                    var picFull = new PictureBox
                    {
                        Image = imageCopy,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Dock = DockStyle.Fill
                    };

                    // Handle form closing to dispose the image copy
                    imageViewer.FormClosed += (s, args) => 
                    {
                        picFull.Image?.Dispose();
                        picFull.Image = null;
                    };

                    imageViewer.Controls.Add(picFull);
                    imageViewer.ShowDialog();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error displaying full image: {ex.Message}");
                }
            }
        }

        private void BtnCaptureProcess_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("📸 Bắt đầu Capture & Process workflow...");

                // Kiểm tra xem đã chọn cửa sổ game chưa
                if (_selectedGameWindow == null)
                {
                    LogMessage("❌ Chưa chọn cửa sổ game. Vui lòng chọn cửa sổ trước.");
                    return;
                }

                // Bước 1: Capture toàn bộ cửa sổ game
                LogMessage("🖼️ Bước 1: Capture toàn bộ cửa sổ game...");
                Rectangle fullWindowArea = new Rectangle(0, 0, _selectedGameWindow!.Bounds.Width, _selectedGameWindow.Bounds.Height);
                
                using (Bitmap? fullWindowCapture = ScreenCapture.CaptureWindow(_selectedGameWindow.Handle, fullWindowArea))
                {
                    if (fullWindowCapture == null)
                    {
                        LogMessage("❌ Không thể capture cửa sổ game");
                        return;
                    }

                    // Lưu ảnh toàn bộ cửa sổ
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    string fullWindowPath = Path.Combine("captcha_workflow", $"full_window_{timestamp}.jpg");
                    
                    Directory.CreateDirectory("captcha_workflow");
                    ScreenCapture.SaveOptimizedImage(fullWindowCapture, fullWindowPath);
                    
                    LogMessage($"✅ Đã lưu ảnh toàn bộ cửa sổ: {fullWindowPath}");
                    LogMessage($"📏 Kích thước cửa sổ: {fullWindowCapture.Width}x{fullWindowCapture.Height}");

                    // Bước 2: Cắt vùng captcha dựa trên tọa độ từ hình ảnh bạn gửi
                    LogMessage("✂️ Bước 2: Cắt vùng captcha theo tọa độ chính xác...");
                    
                    // Tọa độ từ hình ảnh bạn gửi: top=306, bottom=403, left=539, right=539
                    // Nhưng right=539 có vẻ không đúng, có thể là right=741 (left + width)
                    int captchaTop = 306;
                    int captchaBottom = 403;
                    int captchaLeft = 539;
                    int captchaRight = 741; // Ước tính từ hình ảnh
                    
                    // Tính toán vùng captcha
                    int captchaWidth = captchaRight - captchaLeft;
                    int captchaHeight = captchaBottom - captchaTop;
                    
                    LogMessage($"📍 Vùng captcha: X={captchaLeft}, Y={captchaTop}, W={captchaWidth}, H={captchaHeight}");
                    
                    // Kiểm tra xem vùng captcha có nằm trong cửa sổ không
                    if (captchaLeft < 0 || captchaTop < 0 || 
                        captchaRight > fullWindowCapture.Width || captchaBottom > fullWindowCapture.Height)
                    {
                        LogMessage($"❌ Vùng captcha nằm ngoài cửa sổ game!");
                        LogMessage($"🔍 Cửa sổ: {fullWindowCapture.Width}x{fullWindowCapture.Height}");
                        LogMessage($"🔍 Captcha: X={captchaLeft}, Y={captchaTop}, W={captchaWidth}, H={captchaHeight}");
                        return;
                    }

                    // Cắt vùng captcha
                    Rectangle captchaArea = new Rectangle(captchaLeft, captchaTop, captchaWidth, captchaHeight);
                    using (Bitmap captchaCrop = new Bitmap(captchaArea.Width, captchaArea.Height))
                    {
                        using (Graphics g = Graphics.FromImage(captchaCrop))
                        {
                            g.DrawImage(fullWindowCapture, new Rectangle(0, 0, captchaArea.Width, captchaArea.Height), 
                                       captchaArea, GraphicsUnit.Pixel);
                        }

                        // Lưu ảnh captcha đã cắt
                        string captchaPath = Path.Combine("captcha_workflow", $"captcha_crop_{timestamp}.jpg");
                        ScreenCapture.SaveOptimizedImage(captchaCrop, captchaPath);
                        
                        LogMessage($"✅ Đã lưu ảnh captcha đã cắt: {captchaPath}");
                        LogMessage($"📏 Kích thước captcha: {captchaCrop.Width}x{captchaCrop.Height}");

                        // Bước 2.5: Phân tích chất lượng ảnh captcha
                        LogMessage("🔍 Bước 2.5: Phân tích chất lượng ảnh captcha...");
                        AnalyzeCaptchaImage(captchaCrop, timestamp);

                        // Bước 3: Xử lý OCR trên ảnh captcha đã cắt với nhiều phương pháp
                        LogMessage("🔤 Bước 3: Xử lý OCR trên ảnh captcha với nhiều phương pháp...");
                        
                        string tessDataPath = "./tessdata";
                        string language = "eng";
                        string finalResult = "";
                        
                        // Phương pháp 1: OCR trực tiếp
                        try
                        {
                            LogMessage("🔍 Phương pháp 1: OCR trực tiếp...");
                            using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                            {
                                string captchaText = reader.ReadCaptcha(captchaCrop);
                                LogMessage($"📝 OCR trực tiếp: '{captchaText}'");
                                
                                if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                {
                                    finalResult = captchaText;
                                    LogMessage($"✅ Phương pháp 1 thành công: '{captchaText}'");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"❌ Phương pháp 1 thất bại: {ex.Message}");
                        }
                        
                        // Phương pháp 2: OCR với ảnh đã scale lên
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 2: OCR với ảnh scale 3x...");
                                using (Bitmap scaledImage = new Bitmap(captchaCrop.Width * 3, captchaCrop.Height * 3))
                                {
                                    using (Graphics g = Graphics.FromImage(scaledImage))
                                    {
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        g.DrawImage(captchaCrop, 0, 0, scaledImage.Width, scaledImage.Height);
                                    }
                                    
                                    // Lưu ảnh scale để debug
                                    string scaledPath = Path.Combine("captcha_workflow", $"captcha_scaled_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(scaledImage, scaledPath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(scaledImage);
                                        LogMessage($"📝 OCR scale 3x: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 2 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 2 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 3: OCR với ảnh đảo màu
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 3: OCR với ảnh đảo màu...");
                                using (Bitmap invertedImage = ScreenCapture.InvertImage(captchaCrop))
                                {
                                    // Lưu ảnh đảo màu để debug
                                    string invertedPath = Path.Combine("captcha_workflow", $"captcha_inverted_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(invertedImage, invertedPath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(invertedImage);
                                        LogMessage($"📝 OCR đảo màu: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 3 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 3 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 4: OCR với ảnh tăng độ tương phản
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 4: OCR với ảnh tăng độ tương phản...");
                                using (Bitmap contrastImage = ScreenCapture.EnhanceContrast(captchaCrop, 2.0f))
                                {
                                    // Lưu ảnh tăng độ tương phản để debug
                                    string contrastPath = Path.Combine("captcha_workflow", $"captcha_contrast_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(contrastImage, contrastPath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(contrastImage);
                                        LogMessage($"📝 OCR tăng độ tương phản: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 4 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 4 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 5: OCR với preprocessing nâng cao
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 5: OCR với preprocessing nâng cao...");
                                using (Bitmap processedImage = ScreenCapture.PreprocessCaptchaImage(captchaCrop))
                                {
                                    // Lưu ảnh đã preprocessing để debug
                                    string processedPath = Path.Combine("captcha_workflow", $"captcha_processed_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(processedImage, processedPath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(processedImage);
                                        LogMessage($"📝 OCR preprocessing nâng cao: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 5 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 5 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 6: OCR với ảnh scale cực lớn (5x)
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 6: OCR với ảnh scale cực lớn (5x)...");
                                using (Bitmap megaScaledImage = new Bitmap(captchaCrop.Width * 5, captchaCrop.Height * 5))
                                {
                                    using (Graphics g = Graphics.FromImage(megaScaledImage))
                                    {
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                        g.DrawImage(captchaCrop, 0, 0, megaScaledImage.Width, megaScaledImage.Height);
                                    }
                                    
                                    // Lưu ảnh scale cực lớn để debug
                                    string megaScaledPath = Path.Combine("captcha_workflow", $"captcha_mega_scaled_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(megaScaledImage, megaScaledPath);
                                    
                                    LogMessage($"📏 Kích thước ảnh scale 5x: {megaScaledImage.Width}x{megaScaledImage.Height}");
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(megaScaledImage);
                                        LogMessage($"📝 OCR scale 5x: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 6 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 6 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 7: OCR với xử lý màu sắc chuyên biệt
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 7: OCR với xử lý màu sắc chuyên biệt...");
                                using (Bitmap colorProcessedImage = ProcessCaptchaColors(captchaCrop))
                                {
                                    // Lưu ảnh đã xử lý màu để debug
                                    string colorProcessedPath = Path.Combine("captcha_workflow", $"captcha_color_processed_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(colorProcessedImage, colorProcessedPath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(colorProcessedImage);
                                        LogMessage($"📝 OCR xử lý màu: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 7 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 7 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 8: OCR với chuyển đổi sang grayscale và threshold
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 8: OCR với grayscale và threshold...");
                                using (Bitmap grayscaleImage = ConvertToGrayscaleWithThreshold(captchaCrop))
                                {
                                    // Lưu ảnh grayscale để debug
                                    string grayscalePath = Path.Combine("captcha_workflow", $"captcha_grayscale_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(grayscaleImage, grayscalePath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(grayscaleImage);
                                        LogMessage($"📝 OCR grayscale: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 8 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 8 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 9: OCR với xử lý đặc biệt cho captcha có khoảng trắng
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 9: OCR đặc biệt cho captcha có khoảng trắng...");
                                using (Bitmap spacedCaptchaImage = ProcessSpacedCaptcha(captchaCrop))
                                {
                                    // Lưu ảnh đã xử lý khoảng trắng để debug
                                    string spacedPath = Path.Combine("captcha_workflow", $"captcha_spaced_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(spacedCaptchaImage, spacedPath);
                                    
                                    // Tạo TesseractReader với cấu hình đặc biệt cho captcha có khoảng trắng
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        // Cấu hình đặc biệt cho captcha có khoảng trắng
                                        reader.SetVariable("tessedit_pageseg_mode", "8"); // Single word
                                        reader.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ");
                                        reader.SetVariable("tessedit_do_invert", "0"); // Không đảo màu
                                        reader.SetVariable("classify_bln_numeric_mode", "0"); // Không chỉ số
                                        
                                        string captchaText = reader.ReadCaptcha(spacedCaptchaImage);
                                        LogMessage($"📝 OCR khoảng trắng: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 9 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 9 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Phương pháp 10: OCR với xử lý màu sắc từng ký tự riêng biệt
                        if (string.IsNullOrWhiteSpace(finalResult))
                        {
                            try
                            {
                                LogMessage("🔍 Phương pháp 10: OCR với xử lý màu sắc từng ký tự...");
                                using (Bitmap charSeparatedImage = SeparateCharactersByColor(captchaCrop))
                                {
                                    // Lưu ảnh đã tách ký tự để debug
                                    string charSeparatedPath = Path.Combine("captcha_workflow", $"captcha_char_separated_{timestamp}.jpg");
                                    ScreenCapture.SaveOptimizedImage(charSeparatedImage, charSeparatedPath);
                                    
                                    using (var reader = new TesseractCaptchaReader(tessDataPath, language))
                                    {
                                        string captchaText = reader.ReadCaptcha(charSeparatedImage);
                                        LogMessage($"📝 OCR tách ký tự: '{captchaText}'");
                                        
                                        if (!string.IsNullOrWhiteSpace(captchaText) && captchaText.Length >= 2)
                                        {
                                            finalResult = captchaText;
                                            LogMessage($"✅ Phương pháp 10 thành công: '{captchaText}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"❌ Phương pháp 10 thất bại: {ex.Message}");
                            }
                        }
                        
                        // Kết quả cuối cùng
                        if (!string.IsNullOrWhiteSpace(finalResult))
                        {
                            // Lưu kết quả OCR vào file
                            string resultPath = Path.Combine("captcha_workflow", $"ocr_result_{timestamp}.txt");
                            File.WriteAllText(resultPath, finalResult);
                            
                            // Cập nhật UI
                            _lblCaptcha.Text = $"Captcha: {finalResult}";
                            _lblCaptcha.ForeColor = Color.Green;
                            
                            // Hiển thị ảnh captcha đã cắt
                            DisplayImage(captchaPath, $"Captcha: {finalResult}");
                            
                            LogMessage($"🎉 Đã xử lý thành công captcha: '{finalResult}'");
                            LogMessage($"💾 Đã lưu kết quả OCR: {resultPath}");
                        }
                        else
                        {
                            LogMessage("❌ Tất cả phương pháp OCR đều thất bại");
                            _lblCaptcha.Text = "Captcha: Không đọc được";
                            _lblCaptcha.ForeColor = Color.Red;
                            
                            // Lưu thông tin debug
                            string debugPath = Path.Combine("captcha_workflow", $"debug_info_{timestamp}.txt");
                            string debugInfo = $"Captcha size: {captchaCrop.Width}x{captchaCrop.Height}\n" +
                                              $"All OCR methods failed\n" +
                                              $"Timestamp: {timestamp}\n" +
                                              $"Game window: {_selectedGameWindow?.WindowTitle ?? "N/A"}";
                            File.WriteAllText(debugPath, debugInfo);
                            LogMessage($"💾 Đã lưu thông tin debug: {debugPath}");
                        }
                    }
                }

                LogMessage("🎉 Hoàn thành Capture & Process workflow!");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi trong Capture & Process workflow: {ex.Message}");
            }
        }

        private Bitmap ProcessSpacedCaptcha(Bitmap originalImage)
        {
            try
            {
                // Tạo ảnh với nền trắng và text đen để tăng độ tương phản
                Bitmap processedImage = new Bitmap(originalImage.Width, originalImage.Height);
                
                for (int x = 0; x < originalImage.Width; x++)
                {
                    for (int y = 0; y < originalImage.Height; y++)
                    {
                        Color originalPixel = originalImage.GetPixel(x, y);
                        
                        // Nếu pixel gần với màu trắng -> giữ nguyên
                        if (originalPixel.R > 200 && originalPixel.G > 200 && originalPixel.B > 200)
                        {
                            processedImage.SetPixel(x, y, Color.White);
                        }
                        // Nếu pixel có màu sắc -> chuyển thành đen
                        else if (originalPixel.R < 150 || originalPixel.G < 150 || originalPixel.B < 150)
                        {
                            processedImage.SetPixel(x, y, Color.Black);
                        }
                        else
                        {
                            processedImage.SetPixel(x, y, Color.White);
                        }
                    }
                }
                
                return processedImage;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi xử lý captcha có khoảng trắng: {ex.Message}");
                return new Bitmap(originalImage);
            }
        }
        
        private Bitmap SeparateCharactersByColor(Bitmap originalImage)
        {
            try
            {
                // Tạo ảnh với các ký tự được tách biệt rõ ràng
                Bitmap processedImage = new Bitmap(originalImage.Width, originalImage.Height);
                
                // Tìm các vùng có màu sắc (ký tự)
                List<Rectangle> characterRegions = new List<Rectangle>();
                
                for (int x = 0; x < originalImage.Width; x++)
                {
                    for (int y = 0; y < originalImage.Height; y++)
                    {
                        Color pixel = originalImage.GetPixel(x, y);
                        
                        // Nếu pixel có màu sắc rõ ràng (không phải trắng/xám)
                        if (pixel.R < 200 || pixel.G < 200 || pixel.B < 200)
                        {
                            // Tìm vùng ký tự xung quanh pixel này
                            Rectangle charRegion = FindCharacterRegion(originalImage, x, y);
                            if (charRegion.Width > 5 && charRegion.Height > 5)
                            {
                                characterRegions.Add(charRegion);
                            }
                        }
                    }
                }
                
                // Tạo ảnh với các ký tự được làm nổi bật
                using (Graphics g = Graphics.FromImage(processedImage))
                {
                    g.Clear(Color.White);
                    
                    foreach (var region in characterRegions)
                    {
                        // Vẽ ký tự với màu đen đậm
                        for (int x = region.X; x < region.X + region.Width; x++)
                        {
                            for (int y = region.Y; y < region.Y + region.Height; y++)
                            {
                                if (x < originalImage.Width && y < originalImage.Height)
                                {
                                    Color pixel = originalImage.GetPixel(x, y);
                                    if (pixel.R < 200 || pixel.G < 200 || pixel.B < 200)
                                    {
                                        processedImage.SetPixel(x, y, Color.Black);
                                    }
                                }
                            }
                        }
                    }
                }
                
                return processedImage;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi tách ký tự theo màu sắc: {ex.Message}");
                return new Bitmap(originalImage);
            }
        }
        
        private Rectangle FindCharacterRegion(Bitmap image, int startX, int startY)
        {
            int minX = startX, maxX = startX, minY = startY, maxY = startY;
            
            // Tìm ranh giới của ký tự
            for (int x = Math.Max(0, startX - 10); x < Math.Min(image.Width, startX + 10); x++)
            {
                for (int y = Math.Max(0, startY - 10); y < Math.Min(image.Height, startY + 10); y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    if (pixel.R < 200 || pixel.G < 200 || pixel.B < 200)
                    {
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }
            
            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private Bitmap ProcessCaptchaColors(Bitmap originalImage)
        {
            try
            {
                Bitmap processedImage = new Bitmap(originalImage.Width, originalImage.Height);
                
                // Phân tích màu sắc để tìm màu chủ đạo
                Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();
                int totalPixels = originalImage.Width * originalImage.Height;
                int sampleSize = Math.Min(totalPixels, 2000);
                Random rand = new Random();
                
                for (int i = 0; i < sampleSize; i++)
                {
                    int x = rand.Next(originalImage.Width);
                    int y = rand.Next(originalImage.Height);
                    Color pixel = originalImage.GetPixel(x, y);
                    
                    // Làm tròn màu để nhóm các màu tương tự
                    Color roundedColor = Color.FromArgb(
                        (pixel.R / 32) * 32,
                        (pixel.G / 32) * 32,
                        (pixel.B / 32) * 32
                    );
                    
                    if (colorCounts.ContainsKey(roundedColor))
                        colorCounts[roundedColor]++;
                    else
                        colorCounts[roundedColor] = 1;
                }
                
                // Tìm màu phổ biến nhất (có thể là background)
                if (!colorCounts.Any())
                {
                    LogMessage("⚠️ Phân tích màu sắc thất bại: không có pixel mẫu. Trả về ảnh gốc.");
                    return new Bitmap(originalImage);
                }
                Color mostCommonColor = colorCounts.OrderByDescending(x => x.Value).First().Key;
                
                // Tạo ảnh với độ tương phản cao
                for (int x = 0; x < originalImage.Width; x++)
                {
                    for (int y = 0; y < originalImage.Height; y++)
                    {
                        Color originalPixel = originalImage.GetPixel(x, y);
                        
                        // Tính khoảng cách màu từ màu phổ biến nhất
                        double colorDistance = Math.Sqrt(
                            Math.Pow(originalPixel.R - mostCommonColor.R, 2) +
                            Math.Pow(originalPixel.G - mostCommonColor.G, 2) +
                            Math.Pow(originalPixel.B - mostCommonColor.B, 2)
                        );
                        
                        // Nếu màu gần với màu phổ biến -> đen (background)
                        // Nếu màu khác biệt -> trắng (text)
                        if (colorDistance < 100)
                        {
                            processedImage.SetPixel(x, y, Color.Black);
                        }
                        else
                        {
                            processedImage.SetPixel(x, y, Color.White);
                        }
                    }
                }
                
                return processedImage;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi xử lý màu sắc: {ex.Message}");
                return new Bitmap(originalImage);
            }
        }
        
        private Bitmap ConvertToGrayscaleWithThreshold(Bitmap originalImage)
        {
            try
            {
                Bitmap grayscaleImage = new Bitmap(originalImage.Width, originalImage.Height);
                
                // Chuyển sang grayscale
                for (int x = 0; x < originalImage.Width; x++)
                {
                    for (int y = 0; y < originalImage.Height; y++)
                    {
                        Color originalPixel = originalImage.GetPixel(x, y);
                        int grayValue = (int)(originalPixel.R * 0.299 + originalPixel.G * 0.587 + originalPixel.B * 0.114);
                        grayscaleImage.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                    }
                }
                
                // Tính threshold tự động (Otsu's method)
                int threshold = CalculateOtsuThreshold(grayscaleImage);
                
                // Áp dụng threshold để tạo ảnh đen trắng
                Bitmap binaryImage = new Bitmap(grayscaleImage.Width, grayscaleImage.Height);
                for (int x = 0; x < grayscaleImage.Width; x++)
                {
                    for (int y = 0; y < grayscaleImage.Height; y++)
                    {
                        Color pixel = grayscaleImage.GetPixel(x, y);
                        if (pixel.R > threshold)
                        {
                            binaryImage.SetPixel(x, y, Color.White);
                        }
                        else
                        {
                            binaryImage.SetPixel(x, y, Color.Black);
                        }
                    }
                }
                
                grayscaleImage.Dispose();
                return binaryImage;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi chuyển đổi grayscale: {ex.Message}");
                return new Bitmap(originalImage);
            }
        }
        
        private int CalculateOtsuThreshold(Bitmap grayscaleImage)
        {
            try
            {
                // Tạo histogram
                int[] histogram = new int[256];
                for (int x = 0; x < grayscaleImage.Width; x++)
                {
                    for (int y = 0; y < grayscaleImage.Height; y++)
                    {
                        Color pixel = grayscaleImage.GetPixel(x, y);
                        histogram[pixel.R]++;
                    }
                }
                
                int totalPixels = grayscaleImage.Width * grayscaleImage.Height;
                int sum = 0;
                for (int i = 0; i < 256; i++)
                {
                    sum += i * histogram[i];
                }
                
                int sumB = 0;
                int wB = 0;
                int wF = 0;
                double varMax = 0;
                int threshold = 0;
                
                for (int t = 0; t < 256; t++)
                {
                    wB += histogram[t];
                    if (wB == 0) continue;
                    
                    wF = totalPixels - wB;
                    if (wF == 0) break;
                    
                    sumB += t * histogram[t];
                    
                    double mB = (double)sumB / wB;
                    double mF = (double)(sum - sumB) / wF;
                    
                    double varBetween = (double)wB * wF * (mB - mF) * (mB - mF);
                    
                    if (varBetween > varMax)
                    {
                        varMax = varBetween;
                        threshold = t;
                    }
                }
                
                return threshold;
            }
            catch
            {
                return 128; // Fallback threshold
            }
        }

        private void AnalyzeCaptchaImage(Bitmap captchaImage, string timestamp)
        {
            try
            {
                LogMessage("📊 Phân tích chất lượng ảnh captcha...");
                
                // Phân tích màu sắc
                int totalPixels = captchaImage.Width * captchaImage.Height;
                int whitePixels = 0;
                int blackPixels = 0;
                int colorPixels = 0;
                int grayPixels = 0;
                
                // Phân tích một số pixel mẫu
                int sampleSize = Math.Min(totalPixels, 1000);
                Random rand = new Random();
                
                for (int i = 0; i < sampleSize; i++)
                {
                    int x = rand.Next(captchaImage.Width);
                    int y = rand.Next(captchaImage.Height);
                    Color pixel = captchaImage.GetPixel(x, y);
                    
                    // Phân loại pixel
                    if (pixel.R > 240 && pixel.G > 240 && pixel.B > 240)
                        whitePixels++;
                    else if (pixel.R < 15 && pixel.G < 15 && pixel.B < 15)
                        blackPixels++;
                    else if (Math.Abs(pixel.R - pixel.G) < 20 && Math.Abs(pixel.G - pixel.B) < 20 && Math.Abs(pixel.R - pixel.B) < 20)
                        grayPixels++;
                    else
                        colorPixels++;
                }
                
                double whiteRatio = (double)whitePixels / sampleSize;
                double blackRatio = (double)blackPixels / sampleSize;
                double grayRatio = (double)grayPixels / sampleSize;
                double colorRatio = (double)colorPixels / sampleSize;
                
                LogMessage($"🎨 Phân tích màu sắc (mẫu {sampleSize} pixels):");
                LogMessage($"   - Trắng: {whiteRatio:P2}");
                LogMessage($"   - Đen: {blackRatio:P2}");
                LogMessage($"   - Xám: {grayRatio:P2}");
                LogMessage($"   - Màu: {colorRatio:P2}");
                
                // Phân tích độ sáng trung bình
                int totalBrightness = 0;
                for (int i = 0; i < sampleSize; i++)
                {
                    int x = rand.Next(captchaImage.Width);
                    int y = rand.Next(captchaImage.Height);
                    Color pixel = captchaImage.GetPixel(x, y);
                    totalBrightness += (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                }
                
                double avgBrightness = (double)totalBrightness / sampleSize;
                LogMessage($"💡 Độ sáng trung bình: {avgBrightness:F1}/255 ({avgBrightness/255:P1})");
                
                // Đánh giá chất lượng
                bool hasGoodContrast = Math.Abs(whiteRatio - blackRatio) > 0.3;
                bool hasReasonableSize = captchaImage.Width >= 100 && captchaImage.Height >= 50;
                bool hasVariedColors = colorRatio > 0.1 || (whiteRatio > 0.2 && blackRatio > 0.2);
                
                LogMessage($"📈 Đánh giá chất lượng:");
                LogMessage($"   - Có độ tương phản tốt: {hasGoodContrast}");
                LogMessage($"   - Kích thước hợp lý: {hasReasonableSize} ({captchaImage.Width}x{captchaImage.Height})");
                LogMessage($"   - Có màu sắc đa dạng: {hasVariedColors}");
                
                // Lưu thông tin phân tích
                string analysisPath = Path.Combine("captcha_workflow", $"analysis_{timestamp}.txt");
                string analysisInfo = $"Captcha Analysis Report\n" +
                                    $"Timestamp: {timestamp}\n" +
                                    $"Image Size: {captchaImage.Width}x{captchaImage.Height}\n" +
                                    $"Sample Size: {sampleSize}\n\n" +
                                    $"Color Analysis:\n" +
                                    $"  White: {whiteRatio:P2}\n" +
                                    $"  Black: {blackRatio:P2}\n" +
                                    $"  Gray: {grayRatio:P2}\n" +
                                    $"  Color: {colorRatio:P2}\n\n" +
                                    $"Average Brightness: {avgBrightness:F1}/255 ({avgBrightness/255:P1})\n\n" +
                                    $"Quality Assessment:\n" +
                                    $"  Good Contrast: {hasGoodContrast}\n" +
                                    $"  Reasonable Size: {hasReasonableSize}\n" +
                                    $"  Varied Colors: {hasVariedColors}\n\n" +
                                    $"Recommendation: ";
                
                if (hasGoodContrast && hasReasonableSize && hasVariedColors)
                {
                    analysisInfo += "Image quality looks good for OCR";
                    LogMessage("✅ Chất lượng ảnh tốt cho OCR");
                }
                else if (whiteRatio > 0.8)
                {
                    analysisInfo += "Image appears mostly white/blank - no captcha detected";
                    LogMessage("❌ Ảnh chủ yếu màu trắng - có thể không có captcha");
                }
                else if (blackRatio > 0.8)
                {
                    analysisInfo += "Image appears mostly black - no captcha detected";
                    LogMessage("❌ Ảnh chủ yếu màu đen - có thể không có captcha");
                }
                else if (!hasReasonableSize)
                {
                    analysisInfo += "Image too small for reliable OCR";
                    LogMessage("❌ Ảnh quá nhỏ để OCR đáng tin cậy");
                }
                else
                {
                    analysisInfo += "Image quality may be poor for OCR";
                    LogMessage("⚠️ Chất lượng ảnh có thể kém cho OCR");
                }
                
                File.WriteAllText(analysisPath, analysisInfo);
                LogMessage($"💾 Đã lưu báo cáo phân tích: {analysisPath}");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Lỗi phân tích ảnh captcha: {ex.Message}");
            }
        }


    }


}
