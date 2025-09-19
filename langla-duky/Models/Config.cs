using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;

namespace langla_duky.Models
{
    public class Config
    {
        // Remember where this config instance was loaded from so we save back to the same file
        [JsonIgnore]
        private string? _loadedPath;
        // Manual capture settings
        public bool UseManualCapture { get; set; } = true;
        public Rectangle ManualCaptchaArea { get; set; } = new Rectangle(0, 0, 200, 60);
        public Point ManualInputField { get; set; } = new Point(0, 0);
        public Point ManualConfirmButton { get; set; } = new Point(0, 0);

        public string GameWindowTitle { get; set; } = "Làng Lá Duke";
        
        // Suppress UI popups and use logs only
        public bool SilentMode { get; set; } = true;
        
        // Deprecated: Use ManualCaptchaArea instead
        public Rectangle CaptchaArea { get; set; } = new Rectangle(390, 230, 320, 40);
        public Point InputFieldPosition { get; set; } = new Point(650, 430);
        public Point ConfirmButtonPosition { get; set; } = new Point(512, 425);
        
        // Relative (client-area based) coordinates [0..1], prioritized if enabled
        public bool UseRelativeCoordinates { get; set; } = true;
        public RectangleF CaptchaAreaRelative { get; set; } = new RectangleF(0.30f, 0.29f, 0.25f, 0.05f);
        
        // Auto-detect captcha region at runtime (overrides coordinates for the current run)
        public bool AutoDetectCaptchaArea { get; set; } = false;
        
        // Absolute screen coordinates for captcha area (preferred method)
        public int CaptchaLeftX { get; set; } = 540;
        public int CaptchaTopY { get; set; } = 280;
        public int CaptchaRightX { get; set; } = 740;
        public int CaptchaBottomY { get; set; } = 353;
        
        // Absolute screen coordinates for input and button
        public int InputFieldX { get; set; } = 650;
        public int InputFieldY { get; set; } = 430;
        public int ConfirmButtonX { get; set; } = 512;
        public int ConfirmButtonY { get; set; } = 425;
        
        public bool UseAbsoluteCoordinates { get; set; } = false;
        public OCRSettings OCRSettings { get; set; } = new OCRSettings();
        public AutomationSettings AutomationSettings { get; set; } = new AutomationSettings();

        private static string ResolveConfigPath(string filePath)
        {
            try
            {
                if (Path.IsPathRooted(filePath)) return filePath;

                // Priority 1: Current working directory (project root when run from IDE)
                string cwdPath = Path.Combine(Environment.CurrentDirectory, filePath);
                if (File.Exists(cwdPath)) return cwdPath;

                // Priority 2: App base directory (bin output at runtime)
                string baseDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
                if (File.Exists(baseDirPath)) return baseDirPath;

                // Default to CWD for creation if not found anywhere
                return cwdPath;
            }
            catch
            {
                // Last resort: fall back to CWD
                return Path.Combine(Environment.CurrentDirectory, filePath);
            }
        }

        public static Config LoadFromFile(string filePath = "config.json")
        {
            try
            {
                string fullPath = ResolveConfigPath(filePath);
                Console.WriteLine($"Config load path: {fullPath}");
                if (File.Exists(fullPath))
                {
                    string json = File.ReadAllText(fullPath);
                    var config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
                    // Track loaded path so SaveToFile writes back to the same place
                    config._loadedPath = fullPath;
                    Console.WriteLine($"Config loaded - UseManualCapture={config.UseManualCapture} | UseAbsoluteCoordinates={config.UseAbsoluteCoordinates} | UseRelativeCoordinates={config.UseRelativeCoordinates} | SilentMode={config.SilentMode}");
                    return config;
                }

                Console.WriteLine($"Config not found, creating default at: {fullPath}");
                var defaultConfig = new Config();
                defaultConfig._loadedPath = fullPath;
                defaultConfig.SaveToFile(fullPath);
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đọc config: {ex.Message}");
                return new Config();
            }
        }

        public void SaveToFile(string filePath = "config.json")
        {
            try
            {
                // If this instance was loaded from a concrete path, prefer saving back to it
                string fullPath;
                if (Path.IsPathRooted(filePath))
                {
                    fullPath = filePath;
                }
                else if (!string.IsNullOrEmpty(_loadedPath))
                {
                    fullPath = _loadedPath!;
                }
                else
                {
                    fullPath = ResolveConfigPath(filePath);
                }
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(fullPath, json);
                Console.WriteLine($"Config saved to: {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lưu config: {ex.Message}");
            }
        }
    }

    public class OCRSettings
    {
        // CAPTCHA Solver Settings
        public bool UseCapSolver { get; set; } = true; // Use CapSolver
        public string CapSolverAPIKey { get; set; } = "";
        
        // NoCaptchaAI Settings
        public bool UseNoCaptchaAI { get; set; } = false; // Disable NoCaptchaAI by default
        public string NoCaptchaAIAPIKey { get; set; } = "vthuandev-449949f0-7900-6219-5f32-16b41050ca0a";
        
        // Legacy settings (kept for compatibility but not used)
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public string TessdataPath { get; set; } = "./tessdata";
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public string Language { get; set; } = "eng";
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public string CharWhitelist { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public bool UseOCRAPI { get; set; } = false;
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public string OCRAPIProvider { get; set; } = "NoCaptchaAI";
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public string OCRAPIKey { get; set; } = "";
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public int OCRAPITimeout { get; set; } = 30;
        [Obsolete("No longer used - AI CAPTCHA solvers only")]
        public bool FallbackToTesseract { get; set; } = false;
    }

    public class AutomationSettings
    {
        public int DelayBetweenAttempts { get; set; } = 2000;
        public int DelayAfterInput { get; set; } = 500;
        public int DelayAfterClick { get; set; } = 200;
        public int MaxRetries { get; set; } = 3;
    }
}
