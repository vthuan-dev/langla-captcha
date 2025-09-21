# 🎯 **Fix Center Captcha Detection**

## 🔍 **Vấn Đề Đã Xác Định**

### **ROI Detection Sai Vị Trí**
```
❌ Trước: ROI method: auto-detect (client) area={X=863,Y=0,Width=103,Height=58}
```
- **Vấn đề**: ROI ở góc trên bên phải (X=863, Y=0) - KHÔNG phải vị trí captcha thực tế
- **Captcha thực tế**: Nằm ở giữa màn hình game, không phải góc trên
- **Kết quả**: OCR đọc noise thay vì captcha text → kết quả sai `sped`

## 🛠️ **Giải Pháp Đã Triển Khai**

### **1. Cải Thiện Small Captcha Analysis**
```csharp
// Models/AutoCaptchaROIDetector.cs
private double CalculateSmallCaptchaConfidence(System.Drawing.Rectangle region, Mat image)
{
    // STRONGLY prefer center area, avoid edges
    double edgePenalty = 0.0;
    if (centerX > 0.8 || centerY < 0.2) // Top-right area
        edgePenalty = 0.5;
    if (centerX < 0.1 || centerX > 0.9 || centerY < 0.1 || centerY > 0.9) // Any edge
        edgePenalty = 0.3;
    
    // Prefer center area (40%-60% from left, 30%-70% from top)
    double centerScore = 1.0;
    if (centerX < 0.4 || centerX > 0.6)
        centerScore -= 0.3;
    if (centerY < 0.3 || centerY > 0.7)
        centerScore -= 0.3;
}
```

### **2. Thêm Center-Focused Detection Method**
```csharp
// Method 7: Center-Focused Detection (New - prioritize center area)
private ROIDetectionResult DetectWithCenterFocus(Mat screenshot)
{
    // Focus on center area (30%-70% from left, 20%-80% from top)
    var centerX = screenshot.Width / 2;
    var centerY = screenshot.Height / 2;
    var searchWidth = (int)(screenshot.Width * 0.4); // 40% of screen width
    var searchHeight = (int)(screenshot.Height * 0.6); // 60% of screen height
    
    // Search only in center area, avoid edges completely
    using var centerROI = new Mat(gray, new OpenCvSharp.Rect(searchX, searchY, searchWidth, searchHeight));
}
```

### **3. Center-Focused Confidence Calculation**
```csharp
private double CalculateCenterFocusedConfidence(System.Drawing.Rectangle region, Mat image)
{
    // STRONG center preference (40%-60% from left, 30%-70% from top)
    double centerX = (double)region.X / image.Width;
    double centerY = (double)region.Y / image.Height;
    
    double centerScore = 1.0;
    // Perfect center gets highest score
    if (centerX >= 0.4 && centerX <= 0.6 && centerY >= 0.3 && centerY <= 0.7)
        centerScore = 1.0;
    else
    {
        // Distance from ideal center
        double idealX = 0.5;
        double idealY = 0.5;
        double distance = Math.Sqrt(Math.Pow(centerX - idealX, 2) + Math.Pow(centerY - idealY, 2));
        centerScore = Math.Max(0, 1.0 - distance * 2); // Penalize distance from center
    }
}
```

## 🎯 **Kết Quả Mong Đợi**

### **ROI Detection**
- ✅ **Vị trí chính xác**: ROI ở giữa màn hình thay vì góc trên bên phải
- ✅ **Tránh edges**: Không detect ở X=863, Y=0 nữa
- ✅ **Confidence cao hơn**: > 70% cho center regions
- ✅ **Method mới**: "Center-Focused Detection" sẽ được ưu tiên

### **OCR Processing**
- ✅ **Kết quả chính xác**: Đọc captcha text thực tế thay vì noise
- ✅ **Confidence cao hơn**: > 50% thay vì 0.0%
- ✅ **Auto-fill chính xác**: Điền đúng captcha vào input field

## 📊 **Expected Log Output**

### **Trước (Sai)**
```
🎯 ROI Detection: Small Captcha Analysis, Confidence: 63.8%
ROI method: auto-detect (client) area={X=863,Y=0,Width=103,Height=58}
✅ Captcha solved: 'sped' (Confidence: 0.0%, Method: Adaptive Threshold (Tesseract))
```

### **Sau (Đúng)**
```
🎯 ROI Detection: Center-Focused Detection, Confidence: 85.2%
ROI method: auto-detect (client) area={X=640,Y=300,Width=120,Height=45}
✅ Captcha solved: 'jClO' (Confidence: 78.5%, Method: Center-Focused Detection)
```

## 🔧 **Cách Test**

### **1. Chạy Script Test**
```bash
test_center_captcha_detection.bat
```

### **2. Kiểm Tra Logs**
- Tìm `"Center-Focused Detection"` method
- ROI coordinates nên ở center area (X=500-800, Y=200-500)
- Confidence > 70%
- OCR result gần với captcha thực tế

### **3. Debug Images**
- `center_focused_*.png` - Center-focused detection results
- `small_highres_*.png` - High-resolution scaling results
- `small_colorsep_*.png` - Color separation results

## 🚀 **Workflow Mới**

1. **ROI Detection**: 7 methods, ưu tiên center-focused
2. **Center Priority**: Tránh edges, focus vào center area
3. **Better OCR**: Đọc captcha text thực tế
4. **Accurate Auto-fill**: Điền đúng captcha vào game

## 📝 **Technical Details**

### **Search Area**
- **Width**: 40% of screen width (centered)
- **Height**: 60% of screen height (centered)
- **Position**: 30%-70% from left, 20%-80% from top

### **Confidence Scoring**
- **Size**: 25% (prefer 100-150px width, 30-60px height)
- **Aspect Ratio**: 20% (prefer 2.5-4.0)
- **Center Position**: 30% (strong preference for center)
- **Edge Penalty**: -50% for top-right, -30% for any edge

### **Contrast Threshold**
- **Center Area**: stddev > 15 (lower threshold)
- **Edge Areas**: stddev > 20 (higher threshold)

---

**Tóm lại**: Đã fix ROI detection để tìm captcha ở giữa màn hình thay vì góc trên bên phải. Kết quả mong đợi là phát hiện chính xác vị trí captcha và OCR accuracy cao hơn đáng kể.
