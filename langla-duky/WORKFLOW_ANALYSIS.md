# Phân Tích Workflow Tool Captcha

## 📊 Workflow Hiện Tại

### Phase 1: Initialization
1. **MainForm khởi động** → Load config → Init services
2. **User chọn game window** → "Duke Client - By iamDuke"
3. **Start preview** → Hiển thị game window realtime

### Phase 2: Detection (One-shot Mode)
1. **User click Start** → Chạy one-shot detection (không continuous)
2. **CaptchaAutomationService.CheckForCaptchaAsync()**
   - Timeout: 5 giây
   - Gọi CaptchaMonitoringService

3. **CaptchaMonitoringService.CheckForCaptchaDialogAsync()**
   - Method 1: Pattern matching - Check dialog UI
   - Method 2: Area changes - So sánh screenshot
   - Method 3: Color pattern - Phân tích màu captcha
   - Method 4: Direct OCR ✅ - **DETECTED HERE**

### Phase 3: Processing 
1. **CaptchaAutomationService.ProcessCaptchaAsync()**
   - Load config ✅
   - Validate window ✅
   - Get window bounds (1280x750) ✅
   - **❌ TREO TẠI: Capture full window** (Line 137-138)

## 🔴 Vấn Đề Chính

### Nguyên nhân treo:
1. **ScreenCapture.CaptureWindowClientArea()** không có timeout
2. **PrintWindow API** có thể bị block với game window
3. **Fallback to CopyFromScreen** cũng có thể fail

### Log timeline:
```
14:16:12 - Window bounds: 1280x750
14:18:12 - ProcessCaptcha: Canceled (2 phút timeout!)
```

## 🛠️ Đã Sửa

1. **Thêm timeout 5 giây cho screen capture**
2. **Wrap trong Task.Run** để chạy background
3. **Proper error handling** với try-catch

## 📝 Config Hiện Tại

```json
{
  "UseAbsoluteCoordinates": true,
  "CaptchaArea": {
    "X": 669, "Y": 451,
    "Width": 199, "Height": 73
  },
  "OCRSettings": {
    "UseOCRAPI": true,
    "OCRAPIKey": "K87601025288957"
  }
}
```

## 🚀 Workflow Đề Xuất

### Cải thiện Performance:
1. **Giảm window capture size** - Chỉ capture vùng cần thiết
2. **Cache window handle** - Tránh lookup nhiều lần
3. **Parallel OCR** - Chạy multiple OCR methods đồng thời
4. **Smart retry** - Retry với different capture methods

### Monitoring Improvements:
1. **Adaptive interval** - Tăng/giảm tần suất check
2. **Smart detection** - Học pattern của captcha
3. **Resource pooling** - Reuse bitmap objects

## 📊 Flow Diagram

```
User Click Start
    ↓
CheckForCaptchaAsync (5s timeout)
    ↓
4 Detection Methods (parallel)
    ↓
If Detected → ProcessCaptchaAsync
    ↓
Capture Window (5s timeout) ← FIXED
    ↓
Crop Captcha Area
    ↓
OCR (API or Tesseract)
    ↓
Input Text → Click Confirm
    ↓
Verify Response
```

## 🎯 Success Metrics

- Detection rate: ~90% (cần test thêm)
- Processing time: 2-5 seconds (target)
- Success rate: Unknown (cần verify response)
- Resource usage: Medium (cần optimize bitmap)
