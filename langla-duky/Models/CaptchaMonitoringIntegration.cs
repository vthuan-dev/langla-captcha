using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace langla_duky.Models
{
    /// <summary>
    /// Integration class to add captcha monitoring controls to MainForm
    /// </summary>
    public static class CaptchaMonitoringIntegration
    {
        public static void AddMonitoringControls(Form mainForm, IntPtr gameWindowHandle)
        {
            // Create monitoring service
            var monitorService = new CaptchaMonitorService(gameWindowHandle);
            
            // Create monitoring panel
            var monitoringPanel = CreateMonitoringPanel(monitorService);
            
            // Add to main form
            mainForm.Controls.Add(monitoringPanel);
            
            // Handle form closing
            mainForm.FormClosing += (s, e) => monitorService.Dispose();
        }

        private static Panel CreateMonitoringPanel(CaptchaMonitorService monitorService)
        {
            var panel = new Panel
            {
                Name = "CaptchaMonitoringPanel",
                Location = new Point(10, 200), // Adjust position as needed
                Size = new Size(400, 200), // Increased height for template capture
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightGray
            };

            // Title
            var titleLabel = new Label
            {
                Text = "🤖 Auto Captcha Monitoring",
                Location = new Point(10, 10),
                Size = new Size(200, 20),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            panel.Controls.Add(titleLabel);

            // Start/Stop buttons
            var startButton = new Button
            {
                Text = "▶️ Start Monitoring",
                Location = new Point(10, 40),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            panel.Controls.Add(startButton);

            var stopButton = new Button
            {
                Text = "⏹️ Stop Monitoring",
                Location = new Point(140, 40),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            panel.Controls.Add(stopButton);

            // Set up event handlers after all buttons are created
            startButton.Click += (s, e) => StartMonitoring(monitorService, startButton, stopButton);
            stopButton.Click += (s, e) => StopMonitoring(monitorService, startButton, stopButton);

            // Status label
            var statusLabel = new Label
            {
                Text = "Status: Stopped",
                Location = new Point(10, 80),
                Size = new Size(380, 20),
                Font = new Font("Arial", 9)
            };
            panel.Controls.Add(statusLabel);

            // Template Capture Button
            var captureButton = new Button
            {
                Text = "📸 Capture Template",
                Location = new Point(270, 40),
                Size = new Size(120, 30),
                BackColor = Color.LightBlue
            };
            panel.Controls.Add(captureButton);
            
            // Template Status Button
            var statusButton = new Button
            {
                Text = "📊 Template Status",
                Location = new Point(270, 75),
                Size = new Size(120, 25),
                BackColor = Color.LightYellow
            };
            panel.Controls.Add(statusButton);
            
            // Log textbox
            var logTextBox = new TextBox
            {
                Location = new Point(10, 105),
                Size = new Size(380, 60), // Increased height
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8)
            };
            panel.Controls.Add(logTextBox);
            
            // Set up button event handlers
            captureButton.Click += (s, e) => CaptureTemplate(monitorService, logTextBox);
            statusButton.Click += (s, e) => ShowTemplateStatus(logTextBox);

            // Subscribe to events
            monitorService.StatusChanged += (s, status) =>
            {
                if (statusLabel.InvokeRequired)
                {
                    statusLabel.Invoke(new Action(() => UpdateStatus(statusLabel, logTextBox, status)));
                }
                else
                {
                    UpdateStatus(statusLabel, logTextBox, status);
                }
            };

            monitorService.CaptchaSolved += (s, e) =>
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => AddLog(logTextBox, $"✅ SOLVED: '{e.CaptchaText}' ({e.Confidence:F1}%)")));
                }
                else
                {
                    AddLog(logTextBox, $"✅ SOLVED: '{e.CaptchaText}' ({e.Confidence:F1}%)");
                }
            };

            monitorService.CaptchaFailed += (s, e) =>
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => AddLog(logTextBox, $"❌ FAILED: {e.Error}")));
                }
                else
                {
                    AddLog(logTextBox, $"❌ FAILED: {e.Error}");
                }
            };

            return panel;
        }

        private static void StartMonitoring(CaptchaMonitorService monitorService, Button startButton, Button stopButton)
        {
            monitorService.StartMonitoring(2000); // 2 second interval
            startButton.Enabled = false;
            stopButton.Enabled = true;
        }

        private static void StopMonitoring(CaptchaMonitorService monitorService, Button startButton, Button stopButton)
        {
            monitorService.StopMonitoring();
            startButton.Enabled = true;
            stopButton.Enabled = false;
        }
        
        private static void CaptureTemplate(CaptchaMonitorService monitorService, TextBox logTextBox)
        {
            try
            {
                AddLog(logTextBox, "📸 Starting template capture...");
                
                // Get game window handle from monitor service (we need to expose this)
                // For now, we'll use a simple approach
                var gameWindowHandle = GetGameWindowHandle();
                
                if (gameWindowHandle == IntPtr.Zero)
                {
                    AddLog(logTextBox, "❌ Could not find game window handle");
                    return;
                }
                
                var templateCapture = new CaptchaTemplateCapture(true);
                bool success = templateCapture.CaptureAndSaveTemplate(gameWindowHandle);
                
                if (success)
                {
                    AddLog(logTextBox, "✅ Template captured successfully!");
                    AddLog(logTextBox, "🎯 Image Comparison will now be used for faster detection");
                }
                else
                {
                    AddLog(logTextBox, "❌ Template capture failed");
                    AddLog(logTextBox, "💡 Make sure captcha is visible on screen");
                }
                
                templateCapture.Dispose();
            }
            catch (Exception ex)
            {
                AddLog(logTextBox, $"❌ Template capture error: {ex.Message}");
            }
        }
        
        private static void ShowTemplateStatus(TextBox logTextBox)
        {
            try
            {
                AddLog(logTextBox, "📊 Checking template status...");
                
                var imageComparison = new ImageComparisonDetector("captcha_templates", true);
                string status = imageComparison.GetTemplateStatus();
                
                AddLog(logTextBox, status);
                
                if (imageComparison.HasTemplates())
                {
                    AddLog(logTextBox, "✅ Templates are ready for Image Comparison!");
                }
                else
                {
                    AddLog(logTextBox, "❌ No templates found. Click 'Capture Template' to create them.");
                }
                
                imageComparison.Dispose();
            }
            catch (Exception ex)
            {
                AddLog(logTextBox, $"❌ Error checking template status: {ex.Message}");
            }
        }
        
        private static IntPtr GetGameWindowHandle()
        {
            // This is a simplified approach - in real implementation, 
            // you'd get this from the actual game window
            // For now, return the active window
            return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        }

        private static void UpdateStatus(Label statusLabel, TextBox logTextBox, string status)
        {
            statusLabel.Text = $"Status: {status}";
            AddLog(logTextBox, $"[{DateTime.Now:HH:mm:ss}] {status}");
        }

        private static void AddLog(TextBox logTextBox, string message)
        {
            if (logTextBox.Lines.Length > 50) // Keep only last 50 lines
            {
                var lines = logTextBox.Lines;
                var newLines = new string[lines.Length - 1];
                Array.Copy(lines, 1, newLines, 0, newLines.Length);
                logTextBox.Lines = newLines;
            }

            logTextBox.AppendText(message + Environment.NewLine);
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }
    }
}
