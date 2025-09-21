using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace langla_duky.Models
{
    public static class FileBasedMonitoringIntegration
    {
        public static void AddFileBasedMonitoringToForm(Form mainForm, FileBasedCaptchaMonitor fileMonitor)
        {
            var panel = CreateFileBasedMonitoringPanel(fileMonitor);
            mainForm.Controls.Add(panel);
        }

        private static Panel CreateFileBasedMonitoringPanel(FileBasedCaptchaMonitor fileMonitor)
        {
            var panel = new Panel
            {
                Name = "FileBasedMonitoringPanel",
                Location = new Point(10, 420), // Below the existing monitoring panel
                Size = new Size(400, 180),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightCyan
            };

            // Title
            var titleLabel = new Label
            {
                Text = "📂 File-Based Captcha Monitoring",
                Location = new Point(10, 10),
                Size = new Size(380, 20),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            panel.Controls.Add(titleLabel);

            // Status label
            var statusLabel = new Label
            {
                Text = "⏹️ Stopped",
                Location = new Point(10, 35),
                Size = new Size(200, 20),
                Font = new Font("Arial", 9),
                ForeColor = Color.DarkRed
            };
            panel.Controls.Add(statusLabel);

            // Start button
            var startButton = new Button
            {
                Text = "▶️ Start File Monitor",
                Location = new Point(10, 60),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            panel.Controls.Add(startButton);

            // Stop button
            var stopButton = new Button
            {
                Text = "⏹️ Stop File Monitor",
                Location = new Point(140, 60),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            panel.Controls.Add(stopButton);

            // Open folder button
            var openFolderButton = new Button
            {
                Text = "📁 Open Folder",
                Location = new Point(270, 60),
                Size = new Size(100, 30),
                BackColor = Color.LightBlue
            };
            panel.Controls.Add(openFolderButton);

            // Folder path label
            var folderPathLabel = new Label
            {
                Text = "📂 Folder: capture-compare",
                Location = new Point(10, 95),
                Size = new Size(380, 15),
                Font = new Font("Arial", 8),
                ForeColor = Color.DarkSlateGray
            };
            panel.Controls.Add(folderPathLabel);

            // Log textbox
            var logTextBox = new TextBox
            {
                Location = new Point(10, 115),
                Size = new Size(380, 50),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8),
                BackColor = Color.White
            };
            panel.Controls.Add(logTextBox);

            // Set up button event handlers
            startButton.Click += (s, e) => StartFileMonitoring(fileMonitor, startButton, stopButton, statusLabel, logTextBox);
            stopButton.Click += (s, e) => StopFileMonitoring(fileMonitor, startButton, stopButton, statusLabel, logTextBox);
            openFolderButton.Click += (s, e) => OpenImageFolder(logTextBox);

            // Subscribe to events
            fileMonitor.StatusChanged += (s, status) =>
            {
                if (statusLabel.InvokeRequired)
                {
                    statusLabel.Invoke(new Action(() => UpdateFileStatus(statusLabel, logTextBox, status)));
                }
                else
                {
                    UpdateFileStatus(statusLabel, logTextBox, status);
                }
            };

            fileMonitor.CaptchaDetected += (s, e) =>
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => AddFileLog(logTextBox, $"🎯 DETECTED: (Region: {e.Region}, Confidence: {e.Confidence:F1}%)")));
                }
                else
                {
                    AddFileLog(logTextBox, $"🎯 DETECTED: (Region: {e.Region}, Confidence: {e.Confidence:F1}%)");
                }
            };

            fileMonitor.CaptchaSolved += (s, e) =>
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => AddFileLog(logTextBox, $"✅ SOLVED: '{e.CaptchaText}' (Confidence: {e.Confidence:F1}%)")));
                }
                else
                {
                    AddFileLog(logTextBox, $"✅ SOLVED: '{e.CaptchaText}' (Confidence: {e.Confidence:F1}%)");
                }
            };

            fileMonitor.CaptchaFailed += (s, e) =>
            {
                if (logTextBox.InvokeRequired)
                {
                    logTextBox.Invoke(new Action(() => AddFileLog(logTextBox, $"❌ FAILED: {e.Error}")));
                }
                else
                {
                    AddFileLog(logTextBox, $"❌ FAILED: {e.Error}");
                }
            };

            return panel;
        }

        private static void StartFileMonitoring(FileBasedCaptchaMonitor fileMonitor, Button startButton, Button stopButton, Label statusLabel, TextBox logTextBox)
        {
            try
            {
                fileMonitor.StartMonitoring(2000); // 2 second interval
                
                startButton.Enabled = false;
                stopButton.Enabled = true;
                statusLabel.Text = "🔄 Running";
                statusLabel.ForeColor = Color.DarkGreen;
                
                AddFileLog(logTextBox, "🚀 File-based monitoring started");
                AddFileLog(logTextBox, "📂 Monitoring folder: capture-compare");
                AddFileLog(logTextBox, "⏱️ Check interval: 2 seconds");
            }
            catch (Exception ex)
            {
                AddFileLog(logTextBox, $"❌ Error starting file monitoring: {ex.Message}");
            }
        }

        private static void StopFileMonitoring(FileBasedCaptchaMonitor fileMonitor, Button startButton, Button stopButton, Label statusLabel, TextBox logTextBox)
        {
            try
            {
                fileMonitor.StopMonitoring();
                
                startButton.Enabled = true;
                stopButton.Enabled = false;
                statusLabel.Text = "⏹️ Stopped";
                statusLabel.ForeColor = Color.DarkRed;
                
                AddFileLog(logTextBox, "⏹️ File-based monitoring stopped");
            }
            catch (Exception ex)
            {
                AddFileLog(logTextBox, $"❌ Error stopping file monitoring: {ex.Message}");
            }
        }

        private static void OpenImageFolder(TextBox logTextBox)
        {
            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "capture-compare");
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    AddFileLog(logTextBox, $"📁 Created folder: {folderPath}");
                }
                
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
                AddFileLog(logTextBox, $"📂 Opened folder: {folderPath}");
            }
            catch (Exception ex)
            {
                AddFileLog(logTextBox, $"❌ Error opening folder: {ex.Message}");
            }
        }

        private static void UpdateFileStatus(Label statusLabel, TextBox logTextBox, string status)
        {
            statusLabel.Text = status;
            AddFileLog(logTextBox, status);
        }

        private static void AddFileLog(TextBox logTextBox, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] {message}";
            
            logTextBox.AppendText(logMessage + Environment.NewLine);
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }
    }
}
