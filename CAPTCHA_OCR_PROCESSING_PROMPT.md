# 🎯 Prompt: Xử Lý Captcha OCR với OpenCV + Tesseract

## 📋 **Yêu Cầu Chính**

**Mục tiêu:** Tạo function xử lý captcha 4 chữ cái từ ảnh đã có sẵn với workflow đơn giản:
```
Captcha Image → OpenCV (Chuyển thành ảnh trắng đen) → Tesseract (Đọc mã) → Return kết quả
```

**⚠️ LƯU Ý:** 
- Captcha chỉ có **4 chữ cái** (không có số)
- Không cần quan tâm đến việc capture position, chỉ focus vào xử lý ảnh captcha đã có

## 🔧 **Function Signature**

```csharp
public string ProcessCaptchaImage(Bitmap captchaImage)
{
    // Input: Bitmap captcha image (VD: ảnh có text "rzjf" - 4 chữ cái)
    // Output: String chứa captcha text (VD: "rzjf")
    // Return: "" nếu không đọc được
}
```

## 📸 **OpenCV Processing Pipeline**

### **1. Image Preprocessing Steps:**
```csharp
// Step 1: Convert to grayscale
Cv2.CvtColor(matColor, matGray, ColorConversionCodes.BGR2GRAY);

// Step 2: Apply threshold để tạo ảnh trắng đen
Cv2.Threshold(matGray, matBinary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

// Step 3: Clean noise (optional)
Cv2.MorphologyEx(matBinary, matBinary, MorphTypes.Close, kernel);
```

### **2. Multiple Threshold Methods:**
- **Otsu Threshold** (chính)
- **Inverted Otsu** (nếu text sáng trên nền tối)
- **Adaptive Threshold** (nếu có gradient)
- **Fixed Threshold** (nếu biết giá trị threshold)

### **3. Image Enhancement:**
- **Upscale 2-3x** để tăng độ rõ nét
- **Gaussian Blur** để làm mịn
- **Morphological operations** để clean noise

## 🔍 **Tesseract Configuration**

### **1. Engine Setup:**
```csharp
using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);

// Chỉ cho phép chữ cái (không có số)
engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");

// Tối ưu cho captcha
engine.SetVariable("tessedit_pageseg_mode", "8"); // Single word
```

### **2. OCR Processing:**
```csharp
using var page = engine.Process(binaryImage.ToBitmap());
string result = page.GetText().Trim();

// Clean result: chỉ giữ lại chữ cái
result = Regex.Replace(result, @"[^a-zA-Z]", "");
```

## ✅ **Validation Logic**

### **1. Result Validation:**
```csharp
// Kiểm tra độ dài chính xác 4 ký tự
if (result.Length != 4) return "";

// Kiểm tra chỉ chứa chữ cái
if (!Regex.IsMatch(result, @"^[a-zA-Z]+$")) return "";

// Kiểm tra không chứa khoảng trắng
if (result.Contains(" ")) return "";
```

### **2. Quality Check:**
- Độ dài chính xác 4 ký tự
- Chỉ chứa chữ cái (a-z, A-Z)
- Không có khoảng trắng
- Không có ký tự đặc biệt

## 🎯 **Expected Output**

### **Success Cases:**
- Input: Captcha image với "rzjf" → Output: "rzjf"
- Input: Captcha image với "abcd" → Output: "abcd"
- Input: Captcha image với "aBcD" → Output: "aBcD"

### **Failure Cases:**
- Input: Không đọc được → Output: ""
- Input: Đọc được "rz" → Output: "" (quá ngắn, cần 4 ký tự)
- Input: Đọc được "rzjf123" → Output: "" (quá dài, cần 4 ký tự)
- Input: Đọc được "rz jf" → Output: "" (có khoảng trắng)
- Input: Đọc được "rz1f" → Output: "" (có số)

## 📁 **File Structure**

Tạo file mới: `Models/CaptchaImageProcessor.cs`

```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using OpenCvSharp;
using Tesseract;

namespace langla_duky.Models
{
    public class CaptchaImageProcessor
    {
        private readonly string _tessDataPath;
        
        public CaptchaImageProcessor(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
        }
        
        public string ProcessCaptchaImage(Bitmap captchaImage)
        {
            // Implementation here
        }
    }
}
```

## 🧪 **Testing Requirements**

### **1. Test Cases:**
- Captcha với chữ rõ ràng (rzjf, abcd)
- Captcha với chữ hoa/thường (aBcD, xYzW)
- Captcha với noise (nhiễu)
- Captcha với background phức tạp
- Captcha với màu sắc khác nhau

### **2. Test Function:**
```csharp
public void TestCaptchaImageProcessor()
{
    var processor = new CaptchaImageProcessor();
    
    // Test với ảnh mẫu
    using var testImage = new Bitmap("test_captcha.png");
    string result = processor.ProcessCaptchaImage(testImage);
    
    Console.WriteLine($"Result: '{result}'");
    Console.WriteLine($"Valid: {result.Length == 4 && result.All(c => char.IsLetter(c))}");
}
```

## 📊 **Performance Requirements**

- **Processing time:** < 1 giây
- **Memory usage:** < 50MB
- **Success rate:** > 80% với captcha rõ ràng
- **Error handling:** Graceful failure, không crash

## 🔧 **Integration Points**

### **1. Trong MainForm.cs:**
```csharp
// Thay thế SolveCaptchaWithOpenCVAndTesseract()
private string? ProcessCaptchaImage(Bitmap bmp)
{
    var processor = new CaptchaImageProcessor();
    return processor.ProcessCaptchaImage(bmp);
}
```

### **2. Config Update:**
```json
{
  "OCRSettings": {
    "UseImageProcessor": true,
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

1. **File:** `Models/CaptchaImageProcessor.cs`
2. **Test file:** `TestCaptchaImageProcessor.cs`
3. **Integration:** Update MainForm.cs
4. **Documentation:** README cho function

## ⚠️ **Lưu Ý Quan Trọng**

- **Chỉ xử lý chữ cái** (a-z, A-Z) - không có số
- **Độ dài chính xác 4 ký tự**
- **Ưu tiên tốc độ** hơn độ chính xác phức tạp
- **Simple và reliable** hơn complex
- **Test thoroughly** với nhiều loại captcha
- **Không cần quan tâm** đến việc capture position

## 🚀 **Success Criteria**

Function được coi là thành công khi:
- ✅ Xử lý được 80%+ captcha rõ ràng
- ✅ Thời gian xử lý < 1 giây
- ✅ Không crash với input bất kỳ
- ✅ Return đúng format (chỉ chữ cái, chính xác 4 ký tự)
- ✅ Code clean và có documentation

## 📋 **Workflow Summary**

```
Input: Bitmap captcha image (VD: "rzjf")
↓
OpenCV: Grayscale → Threshold → Clean noise
↓
Tesseract: OCR với whitelist chữ+số
↓
Validation: Length 3-8, chỉ chữ+số, không khoảng trắng
↓
Output: String captcha text (VD: "rzjf")
```
