# 🎯 Game Captcha Solver - OpenCV + Tesseract Integration

## 📋 Overview
Advanced captcha solver using OpenCV for image preprocessing and Tesseract for OCR, specifically optimized for game captchas with colored text on white/brown backgrounds.

## 📦 Required NuGet Packages

Add these packages to your `.csproj` file:

```xml
<PackageReference Include="OpenCvSharp4" Version="4.8.0.20230708" />
<PackageReference Include="OpenCvSharp4.Extensions" Version="4.8.0.20230708" />
<PackageReference Include="Tesseract" Version="5.2.0" />
```

Or install via Package Manager Console:
```powershell
Install-Package OpenCvSharp4 -Version 4.8.0.20230708
Install-Package OpenCvSharp4.Extensions -Version 4.8.0.20230708
Install-Package Tesseract -Version 5.2.0
```

## 📁 Setup Instructions

### 1. Download Tesseract Data
```bash
# Create tessdata directory in your project root
mkdir tessdata

# Download eng.traineddata from:
# https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata

# Place the file in: YourProject/tessdata/eng.traineddata
```

### 2. Project Structure
```
YourProject/
├── tessdata/
│   └── eng.traineddata
├── Services/
│   ├── GameCaptchaSolver.cs
│   ├── CaptchaSolverIntegration.cs
│   └── EnhancedCaptchaAutomationService.cs
├── captcha_opencv_debug/     (auto-created for debug images)
├── captcha_test/             (auto-created for test images)
└── Program.cs
```

## 🚀 Quick Start

### Basic Usage
```csharp
using langla_duky.Services;

// Initialize solver
using var solver = new GameCaptchaSolver("./tessdata");

// Simple solving
string result = solver.SolveCaptcha(captchaBytes);
Console.WriteLine($"Result: {result}");

// Detailed solving
var detailedResult = solver.SolveCaptchaDetailed(captchaBytes);
Console.WriteLine($"Text: {detailedResult.Text}");
Console.WriteLine($"Confidence: {detailedResult.Confidence:F1}%");
Console.WriteLine($"Method: {detailedResult.Method}");
Console.WriteLine($"Time: {detailedResult.ProcessingTime.TotalMilliseconds:F0}ms");
```

### Integration with Existing Tool
```csharp
// Replace CaptchaAutomationService with EnhancedCaptchaAutomationService
private EnhancedCaptchaAutomationService _automationService;

private void InitializeServices()
{
    _automationService = new EnhancedCaptchaAutomationService();
    
    // Enable local solver first (OpenCV + Tesseract)
    _automationService.UseLocalSolverFirst = true;
    
    // Subscribe to events
    _automationService.MonitoringStatusChanged += OnMonitoringStatusChanged;
    _automationService.ErrorOccurred += OnErrorOccurred;
    _automationService.CaptchaDetected += OnCaptchaDetected;
    _automationService.CaptchaProcessed += OnCaptchaProcessed;
}

// Test the enhanced solver
private async void TestSolverButton_Click(object sender, EventArgs e)
{
    string result = await _automationService.TestEnhancedSolverAsync();
    MessageBox.Show(result, "Test Result");
}
```

## 🔧 Configuration Options

### Solver Priority
```csharp
var enhancedService = new EnhancedCaptchaAutomationService();

// Use local solver first (recommended for speed and cost)
enhancedService.UseLocalSolverFirst = true;  // OpenCV+Tesseract → AI services

// Use AI services first (recommended for accuracy)
enhancedService.UseLocalSolverFirst = false; // AI services → OpenCV+Tesseract
```

### Debug Options
```csharp
var solver = new GameCaptchaSolver("./tessdata");

// Enable debug image saving
solver.SaveDebugImages = true;
solver.DebugImagePath = "captcha_opencv_debug";
```

## 📊 Performance Characteristics

| Method | Speed | Cost | Accuracy | Best For |
|--------|-------|------|----------|----------|
| OpenCV + Tesseract | ~200-500ms | Free | 70-85% | High volume, cost-sensitive |
| NoCaptchaAI | ~1-3s | $0.0005/solve | 85-95% | High accuracy needed |
| CapSolver | ~1-3s | $0.001/solve | 90-98% | Maximum accuracy |

## 🎯 Preprocessing Methods

The solver uses multiple preprocessing methods with voting system:

1. **Otsu Threshold** - Primary method for most captchas
2. **Adaptive Threshold** - Fallback for varying lighting
3. **HSV Color Filter** - For colored text captchas
4. **Morphological Enhanced** - Advanced noise removal

## 🔍 Troubleshooting

### Common Issues

#### 1. "Tesseract not found" Error
```bash
# Make sure eng.traineddata exists in tessdata folder
ls tessdata/eng.traineddata

# Download if missing:
wget https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata -O tessdata/eng.traineddata
```

#### 2. Poor OCR Results
- Enable debug images: `solver.SaveDebugImages = true`
- Check `captcha_opencv_debug/` folder for preprocessing results
- Adjust preprocessing methods if needed

#### 3. Memory Issues
```csharp
// Ensure proper disposal
using var solver = new GameCaptchaSolver();
// solver automatically disposed at end of using block
```

#### 4. Thread Safety
```csharp
// GameCaptchaSolver is thread-safe for multiple simultaneous calls
var tasks = captchas.Select(async captcha => 
    await Task.Run(() => solver.SolveCaptcha(captcha))
);
var results = await Task.WhenAll(tasks);
```

## 📈 Optimization Tips

### 1. Batch Processing
```csharp
// Process multiple captchas efficiently
var results = await solver.SolveCaptchasBatchAsync(multipleCaptchaBytes);
```

### 2. Caching Strategy
```csharp
// Keep solver instance alive to avoid Tesseract engine reload
private static readonly GameCaptchaSolver _cachedSolver = new GameCaptchaSolver();

public string SolveCaptcha(byte[] imageBytes)
{
    return _cachedSolver.SolveCaptcha(imageBytes);
}
```

### 3. Fallback Strategy
```csharp
// Configure fallback chain
var config = new Config();
config.OCRSettings.UseCapSolver = true;  // Highest accuracy
config.OCRSettings.UseNoCaptchaAI = true;  // Medium accuracy
// Local solver always available as fallback
```

## 🧪 Testing

### Manual Testing
```csharp
// Test with sample image
var solver = new GameCaptchaSolver();
var result = await solver.TestSolverAsync("sample_captcha.png");
Console.WriteLine(result);
```

### Automated Testing
```csharp
// Test current captcha area
var enhancedService = new EnhancedCaptchaAutomationService();
string testResult = await enhancedService.TestEnhancedSolverAsync();
```

## 📝 API Reference

### GameCaptchaSolver Methods
- `SolveCaptcha(byte[] imageBytes)` - Simple solving
- `SolveCaptcha(string imagePath)` - Solve from file
- `SolveCaptchaDetailed(byte[] imageBytes)` - Detailed result
- `SolveCaptchasBatchAsync(IEnumerable<byte[]>)` - Batch processing

### CaptchaResult Properties
- `Text` - Detected text
- `Confidence` - OCR confidence (0-100%)
- `Success` - Whether solving succeeded
- `Method` - Method(s) used for recognition
- `ProcessingTime` - Total processing time

### EnhancedCaptchaAutomationService
- `UseLocalSolverFirst` - Priority setting
- `ProcessCaptchaAsync()` - Enhanced captcha processing
- `TestEnhancedSolverAsync()` - Test current setup

## 🎯 Expected Results

### Success Rate Targets
- **Simple captchas (clear text)**: 85-95%
- **Noisy captchas**: 70-85%
- **Colored captchas**: 75-90%
- **Complex backgrounds**: 60-80%

### Performance Targets
- **Processing time**: <1 second per captcha
- **Memory usage**: <50MB per solver instance
- **Throughput**: 200+ captchas per day

## 🔄 Integration Workflow

```
Original Captcha Image
         ↓
   Capture & Validate
         ↓
    OpenCV Preprocessing
    (4 different methods)
         ↓
   Tesseract OCR Recognition
         ↓
     Voting System
         ↓
    Result Validation
         ↓
   Fallback to AI Services
   (if local solver fails)
         ↓
    Final Result
```

This implementation provides a robust, cost-effective solution for game captcha solving with multiple fallback options and comprehensive error handling.
