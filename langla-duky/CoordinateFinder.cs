using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace langla_duky
{
    public partial class CoordinateFinder : Form
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out Point lpPoint);
        
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        
        private const int VK_F1 = 0x70;
        private const int VK_F2 = 0x71;
        private const int VK_ESCAPE = 0x1B;
        
        private Label? lblPosition;
        private Label? lblCaptchaArea;
        private Label? lblInstructions;
        private System.Windows.Forms.Timer? timer;
        private Point captchaTopLeft = Point.Empty;
        private Point captchaBottomRight = Point.Empty;
        
        public CoordinateFinder()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Coordinate Finder - Press F1/F2 to mark, ESC to close";
            this.Size = new Size(600, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            
            lblInstructions = new Label
            {
                Text = "Instructions:\n" +
                       "1. Move mouse to TOP-LEFT of captcha image, press F1\n" +
                       "2. Move mouse to BOTTOM-RIGHT of captcha image, press F2\n" +
                       "3. Press ESC to close",
                Location = new Point(10, 10),
                Size = new Size(560, 60),
                Font = new Font("Arial", 10)
            };
            this.Controls.Add(lblInstructions);
            
            lblPosition = new Label
            {
                Text = "Current Position: X=0, Y=0",
                Location = new Point(10, 80),
                Size = new Size(300, 20),
                Font = new Font("Consolas", 10)
            };
            this.Controls.Add(lblPosition);
            
            lblCaptchaArea = new Label
            {
                Text = "Captcha Area: Not set",
                Location = new Point(10, 110),
                Size = new Size(560, 40),
                Font = new Font("Consolas", 10),
                ForeColor = Color.Blue
            };
            this.Controls.Add(lblCaptchaArea);
            
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 50;
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        
        private void Timer_Tick(object? sender, EventArgs e)
        {
            Point mousePos;
            GetCursorPos(out mousePos);
            if (lblPosition != null) lblPosition.Text = $"Current Position: X={mousePos.X}, Y={mousePos.Y}";
            
            // Check F1 key
            if ((GetAsyncKeyState(VK_F1) & 0x8000) != 0)
            {
                captchaTopLeft = mousePos;
                UpdateCaptchaArea();
                System.Threading.Thread.Sleep(200); // Debounce
            }
            
            // Check F2 key  
            if ((GetAsyncKeyState(VK_F2) & 0x8000) != 0)
            {
                captchaBottomRight = mousePos;
                UpdateCaptchaArea();
                System.Threading.Thread.Sleep(200); // Debounce
            }
            
            // Check ESC key
            if ((GetAsyncKeyState(VK_ESCAPE) & 0x8000) != 0)
            {
                this.Close();
            }
        }
        
        private void UpdateCaptchaArea()
        {
            if (captchaTopLeft != Point.Empty && captchaBottomRight != Point.Empty)
            {
                int width = captchaBottomRight.X - captchaTopLeft.X;
                int height = captchaBottomRight.Y - captchaTopLeft.Y;
                
                if(lblCaptchaArea != null) lblCaptchaArea.Text = $"Captcha Area: X={captchaTopLeft.X}, Y={captchaTopLeft.Y}, " +
                                     $"Width={width}, Height={height}\n" +
                                     $"Add this to config.json!";
                if(lblCaptchaArea != null) lblCaptchaArea.ForeColor = Color.Green;
                
                // Also copy to clipboard
                string configJson = $"\"CaptchaArea\": {{\n" +
                                  $"  \"X\": {captchaTopLeft.X},\n" +
                                  $"  \"Y\": {captchaTopLeft.Y},\n" +
                                  $"  \"Width\": {width},\n" +
                                  $"  \"Height\": {height}\n" +
                                  $"}}";
                Clipboard.SetText(configJson);
                if(lblCaptchaArea != null) lblCaptchaArea.Text += "\n(Copied to clipboard!)";
            }
            else if (captchaTopLeft != Point.Empty)
            {
                if(lblCaptchaArea != null) lblCaptchaArea.Text = $"Top-Left set: X={captchaTopLeft.X}, Y={captchaTopLeft.Y}\n" +
                                     "Now press F2 at bottom-right corner";
                if(lblCaptchaArea != null) lblCaptchaArea.ForeColor = Color.Orange;
            }
        }
    }
}
