# 🎯 Prompt: Xử Lý Captcha 4 Chữ Số với OpenCV + Tesseract

## ⚠️ **VẤN ĐỀ HIỆN TẠI**

Tool đang capture sai vùng captcha:
- **Config hiện tại:** `CaptchaArea: {X=0,Y=0,Width=200,Height=60}` 
- **Thực tế capture:** `{X=449,Y=220,Width=126,Height=59}`
- **Kết quả:** Chỉ thấy màu trắng, không có captcha text

## 📋 **Yêu Cầu Chính**

**BƯỚC 1:** Sửa config để capture đúng vùng captcha
**BƯỚC 2:** Tạo function xử lý captcha 4 chữ số với workflow đơn giản:
```
Captcha Image → OpenCV (Chuyển thành ảnh trắng đen) → Tesseract (Đọc mã) → Return kết quả
```

## 🔧 **BƯỚC 1: Sửa Config Coordinates**

### **Vấn đề:** 
Config hiện tại capture sai vùng:
```json
{
  "CaptchaArea": {
    "X": 0,        // ❌ Sai - phải là 449
    "Y": 0,        // ❌ Sai - phải là 220  
    "Width": 200,  // ❌ Sai - phải là 126
    "Height": 60   // ❌ Sai - phải là 59
  }
}
```

### **Sửa thành:**
```json
{
  "CaptchaArea": {
    "X": 449,      // ✅ Đúng vị trí X
    "Y": 220,      // ✅ Đúng vị trí Y
    "Width": 126,  // ✅ Đúng chiều rộng
    "Height": 59   // ✅ Đúng chiều cao
  }
}
```

### **Hoặc sử dụng absolute coordinates:**
```json
{
  "UseAbsoluteCoordinates": true,
  "CaptchaLeftX": 449,
  "CaptchaTopY": 220,
  "CaptchaRightX": 575,    // 449 + 126
  "CaptchaBottomY": 279    // 220 + 59
}
```

## 🔧 **BƯỚC 2: Function Signature**

```csharp
public string ProcessCaptcha4Digits(Bitmap captchaImage)
{
    // Input: Bitmap captcha image (4 chữ số)
    // Output: String chứa 4 chữ số (VD: "1234")
    // Return: "" nếu không đọc được
}
```

## 📸 **OpenCV Processing Requirements**

### 1. **Image Preprocessing Pipeline**
```csharp
// Step 1: Convert to grayscale
Cv2.CvtColor(matColor, matGray, ColorConversionCodes.BGR2GRAY);

// Step 2: Apply threshold để tạo ảnh trắng đen
Cv2.Threshold(matGray, matBinary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

// Step 3: Clean noise (optional)
// Morphological operations để loại bỏ noise
```

### 2. **Threshold Methods cần thử**
- **Otsu Threshold** (chính)
- **Inverted Otsu** (nếu text sáng trên nền tối)
- **Adaptive Threshold** (nếu có gradient)
- **Fixed Threshold** (nếu biết giá trị threshold)

### 3. **Image Enhancement**
- **Upscale 2-3x** để tăng độ rõ nét
- **Gaussian Blur** để làm mịn
- **Morphological operations** để clean noise

## 🔍 **Tesseract Configuration**

### 1. **Engine Setup**
```csharp
using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);

// Chỉ cho phép số 0-9
engine.SetVariable("tessedit_char_whitelist", "0123456789");

// Tăng độ chính xác cho số
engine.SetVariable("tessedit_pageseg_mode", "8"); // Single word
```

### 2. **OCR Processing**
```csharp
using var page = engine.Process(binaryImage.ToBitmap());
string result = page.GetText().Trim();

// Clean result: chỉ giữ lại số
result = Regex.Replace(result, @"[^0-9]", "");
```

## ✅ **Validation Logic**

### 1. **Result Validation**
```csharp
// Kiểm tra độ dài
if (result.Length != 4) return "";

// Kiểm tra chỉ chứa số
if (!Regex.IsMatch(result, @"^\d{4}$")) return "";

// Kiểm tra không chứa ký tự đặc biệt
if (result.Any(c => !char.IsDigit(c))) return "";
```

### 2. **Quality Check**
- Độ dài phải = 4
- Chỉ chứa số 0-9
- Không có khoảng trắng
- Không có ký tự đặc biệt

## 🎯 **Expected Output**

### **Success Cases:**
- Input: Captcha image với "1234" → Output: "1234"
- Input: Captcha image với "5678" → Output: "5678"
- Input: Captcha image với "0000" → Output: "0000"

### **Failure Cases:**
- Input: Không đọc được → Output: ""
- Input: Đọc được "12" → Output: "" (không đủ 4 số)
- Input: Đọc được "12ab" → Output: "" (có ký tự không phải số)

## 📁 **File Structure**

Tạo file mới: `Models/Captcha4DigitProcessor.cs`

```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using OpenCvSharp;
using Tesseract;

namespace langla_duky.Models
{
    public class Captcha4DigitProcessor
    {
        private readonly string _tessDataPath;
        
        public Captcha4DigitProcessor(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
        }
        
        public string ProcessCaptcha4Digits(Bitmap captchaImage)
        {
            // Implementation here
        }
    }
}
```

## 🧪 **Testing Requirements**

### 1. **Test Cases**
- Captcha với số rõ ràng (1234, 5678)
- Captcha với số mờ (0000, 1111)
- Captcha với noise (nhiễu)
- Captcha với background phức tạp

### 2. **Test Function**
```csharp
public void TestCaptcha4Digits()
{
    var processor = new Captcha4DigitProcessor();
    
    // Test với ảnh mẫu
    using var testImage = new Bitmap("test_captcha.png");
    string result = processor.ProcessCaptcha4Digits(testImage);
    
    Console.WriteLine($"Result: '{result}'");
    Console.WriteLine($"Valid: {result.Length == 4 && result.All(char.IsDigit)}");
}
```

## 📊 **Performance Requirements**

- **Processing time:** < 1 giây
- **Memory usage:** < 50MB
- **Success rate:** > 80% với captcha rõ ràng
- **Error handling:** Graceful failure, không crash

## 🔧 **Integration Points**

### 1. **Trong MainForm.cs**
```csharp
// Thay thế SolveCaptchaWithOpenCVAndTesseract()
private string? SolveCaptcha4Digits(Bitmap bmp)
{
    var processor = new Captcha4DigitProcessor();
    return processor.ProcessCaptcha4Digits(bmp);
}
```

### 2. **Config Update**
```json
{
  "OCRSettings": {
    "Use4DigitMode": true,
    "TessdataPath": "./tessdata"
  }
}
```

## 📝 **Documentation Requirements**

1. **Code comments** cho mỗi step
2. **Logging** để debug
3. **Error handling** với try-catch
4. **Performance metrics** (thời gian xử lý)

## 🎯 **Deliverables**

1. **Sửa config.json:** Update coordinates đúng vị trí captcha
2. **File:** `Models/Captcha4DigitProcessor.cs`
3. **Test file:** `TestCaptcha4Digits.cs`
4. **Integration:** Update MainForm.cs
5. **Documentation:** README cho function

## 🔍 **Debug Steps**

### **1. Kiểm tra vùng capture:**
- Mở game Duke Client
- Chạy tool và xem log "ROI method: relative (client) area={X=461,Y=227,Width=102,Height=45}"
- Chụp screenshot vùng này để xem có captcha không

### **2. Test với manual coordinates:**
```json
{
  "UseManualCapture": true,
  "ManualCaptchaArea": {
    "X": 449,
    "Y": 220, 
    "Width": 126,
    "Height": 59
  }
}
```

### **3. Verify captcha image:**
- Check file `captcha_debug/captcha_enhanced_*.png`
- Đảm bảo có text captcha trong ảnh
- Nếu chỉ thấy màu trắng → coordinates vẫn sai

## ⚠️ **Lưu Ý Quan Trọng**

### **PRIORITY 1: Fix Coordinates**
- **VẤN ĐỀ CHÍNH:** Tool capture sai vùng → chỉ thấy màu trắng
- **GIẢI PHÁP:** Update config.json với coordinates đúng: `{X=449,Y=220,Width=126,Height=59}`
- **VERIFY:** Check file `captcha_debug/*.png` phải có captcha text

### **PRIORITY 2: OCR Processing**
- **Chỉ xử lý 4 chữ số** (0-9)
- **Không cần xử lý chữ cái**
- **Ưu tiên tốc độ** hơn độ chính xác phức tạp
- **Simple và reliable** hơn complex
- **Test thoroughly** với nhiều loại captcha

### **Debug Checklist:**
- [ ] Config coordinates đúng vị trí captcha
- [ ] Captured image có text captcha (không phải màu trắng)
- [ ] OCR function hoạt động với test image
- [ ] Return đúng format 4 chữ số

## 🚀 **Success Criteria**

Function được coi là thành công khi:
- ✅ Xử lý được 80%+ captcha 4 số rõ ràng
- ✅ Thời gian xử lý < 1 giây
- ✅ Không crash với input bất kỳ
- ✅ Return đúng format (4 chữ số hoặc "")
- ✅ Code clean và có documentation
