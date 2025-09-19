using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace langla_duky.Models
{
    // Custom JsonConverter for System.Drawing.Rectangle
    public class RectangleConverter : JsonConverter<Rectangle>
    {
        public override void WriteJson(JsonWriter writer, Rectangle value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Width");
            writer.WriteValue(value.Width);
            writer.WritePropertyName("Height");
            writer.WriteValue(value.Height);
            writer.WriteEndObject();
        }

        public override Rectangle ReadJson(JsonReader reader, Type objectType, Rectangle existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Rectangle.Empty;

            if (reader.TokenType == JsonToken.StartObject)
            {
                int x = 0, y = 0, width = 0, height = 0;
                
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject)
                        break;
                        
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string propertyName = reader.Value.ToString();
                        reader.Read();
                        
                        switch (propertyName)
                        {
                            case "X":
                                x = Convert.ToInt32(reader.Value);
                                break;
                            case "Y":
                                y = Convert.ToInt32(reader.Value);
                                break;
                            case "Width":
                                width = Convert.ToInt32(reader.Value);
                                break;
                            case "Height":
                                height = Convert.ToInt32(reader.Value);
                                break;
                        }
                    }
                }
                
                return new Rectangle(x, y, width, height);
            }
            
            throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
        }
    }

    // Custom JsonConverter for System.Drawing.Point
    public class PointConverter : JsonConverter<Point>
    {
        public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WriteEndObject();
        }

        public override Point ReadJson(JsonReader reader, Type objectType, Point existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return Point.Empty;

            if (reader.TokenType == JsonToken.StartObject)
            {
                int x = 0, y = 0;
                
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject)
                        break;
                        
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string propertyName = reader.Value.ToString();
                        reader.Read();
                        
                        switch (propertyName)
                        {
                            case "X":
                                x = Convert.ToInt32(reader.Value);
                                break;
                            case "Y":
                                y = Convert.ToInt32(reader.Value);
                                break;
                        }
                    }
                }
                
                return new Point(x, y);
            }
            
            throw new JsonSerializationException($"Unexpected token type: {reader.TokenType}");
        }
    }

    public class Config
    {
        // Remember where this config instance was loaded from so we save back to the same file
        [JsonIgnore]
        private string? _loadedPath;
        
        // Public property to access the loaded path for debugging
        [JsonIgnore]
        public string? LoadedPath => _loadedPath;
        
        // Manual capture settings
        public bool UseManualCapture { get; set; } = true;
        [JsonConverter(typeof(RectangleConverter))]
        public Rectangle ManualCaptchaArea { get; set; } = new Rectangle(0, 0, 200, 60);
        [JsonConverter(typeof(PointConverter))]
        public Point ManualInputField { get; set; } = new Point(0, 0);
        [JsonConverter(typeof(PointConverter))]
        public Point ManualConfirmButton { get; set; } = new Point(0, 0);

        public string GameWindowTitle { get; set; } = "Làng Lá Duke";
        
        // Suppress UI popups and use logs only
        public bool SilentMode { get; set; } = true;
        
        // Deprecated: Use ManualCaptchaArea instead
        [JsonConverter(typeof(RectangleConverter))]
        public Rectangle CaptchaArea { get; set; } = new Rectangle(390, 230, 320, 40);
        [JsonConverter(typeof(PointConverter))]
        public Point InputFieldPosition { get; set; } = new Point(650, 430);
        [JsonConverter(typeof(PointConverter))]
        public Point ConfirmButtonPosition { get; set; } = new Point(512, 425);
        
        // Relative (client-area based) coordinates [0..1], prioritized if enabled
        public bool UseRelativeCoordinates { get; set; } = true;
        public RectangleF CaptchaAreaRelative { get; set; } = new RectangleF(0.36f, 0.303f, 0.08f, 0.06f);
        
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
                Console.WriteLine($"DEBUG: Checking CWD path: {cwdPath} (exists: {File.Exists(cwdPath)})");
                if (File.Exists(cwdPath)) 
                {
                    Console.WriteLine($"DEBUG: Found config at CWD: {cwdPath}");
                    return cwdPath;
                }

                // Priority 2: App base directory (bin output at runtime)
                string baseDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
                Console.WriteLine($"DEBUG: Checking BaseDir path: {baseDirPath} (exists: {File.Exists(baseDirPath)})");
                if (File.Exists(baseDirPath)) 
                {
                    Console.WriteLine($"DEBUG: Found config at BaseDir: {baseDirPath}");
                    return baseDirPath;
                }

                // Default to CWD for creation if not found anywhere
                Console.WriteLine($"DEBUG: No config found, defaulting to CWD: {cwdPath}");
                return cwdPath;
            }
            catch
            {
                // Last resort: fall back to CWD
                string fallbackPath = Path.Combine(Environment.CurrentDirectory, filePath);
                Console.WriteLine($"DEBUG: Exception in ResolveConfigPath, fallback to: {fallbackPath}");
                return fallbackPath;
            }
        }

        public static Config LoadFromFile(string filePath = "config.json")
        {
            try
            {
                Console.WriteLine($"DEBUG: LoadFromFile called with filePath: {filePath}");
                Console.WriteLine($"DEBUG: Environment.CurrentDirectory: {Environment.CurrentDirectory}");
                Console.WriteLine($"DEBUG: AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                
                string fullPath = ResolveConfigPath(filePath);
                Console.WriteLine($"Config load path: {fullPath}");
                Console.WriteLine($"DEBUG: File.Exists({fullPath}): {File.Exists(fullPath)}");
                
                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"DEBUG: About to read file: {fullPath}");
                    string json = File.ReadAllText(fullPath);
                    Console.WriteLine($"Config JSON content: {json}");
                    Console.WriteLine($"DEBUG: About to deserialize JSON");
                    var config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
                    Console.WriteLine($"DEBUG: Deserialization completed");
                    // Track loaded path so SaveToFile writes back to the same place
                    config._loadedPath = fullPath;
                    Console.WriteLine($"Config loaded - UseManualCapture={config.UseManualCapture} | UseAbsoluteCoordinates={config.UseAbsoluteCoordinates} | UseRelativeCoordinates={config.UseRelativeCoordinates} | SilentMode={config.SilentMode}");
                    Console.WriteLine($"DEBUG: Config._loadedPath set to: {config._loadedPath}");
                    return config;
                }

                Console.WriteLine($"Config not found, creating default at: {fullPath}");
                Console.WriteLine($"DEBUG: Creating default config with values: UseManualCapture={new Config().UseManualCapture}, UseAbsoluteCoordinates={new Config().UseAbsoluteCoordinates}, UseRelativeCoordinates={new Config().UseRelativeCoordinates}");
                var defaultConfig = new Config();
                defaultConfig._loadedPath = fullPath;
                defaultConfig.SaveToFile(fullPath);
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception in LoadFromFile: {ex.Message}");
                Console.WriteLine($"ERROR: Stack trace: {ex.StackTrace}");
                Console.WriteLine($"DEBUG: Returning default config due to exception");
                var defaultConfig = new Config();
                defaultConfig._loadedPath = null; // Mark as not loaded from file
                return defaultConfig;
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
