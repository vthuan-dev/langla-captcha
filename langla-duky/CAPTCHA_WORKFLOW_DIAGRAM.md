# 🔄 Workflow Xử Lý CAPTCHA - Langla-Duky

## 📊 Workflow Tổng Thể

```mermaid
graph TD
    A[🚀 Start Application] --> B[📋 Load Config]
    B --> C[🎯 Initialize Tesseract Engine]
    C --> D[🖥️ Select Game Window]
    D --> E[🔍 Auto-Detect CAPTCHA Area]
    
    E --> F{Detection Success?}
    F -->|Yes| G[📸 Capture CAPTCHA Image]
    F -->|No| H[⚠️ Use Manual Coordinates]
    H --> G
    
    G --> I[🔍 Analyze Captured Image]
    I --> J[🖼️ OpenCV Image Processing]
    
    J --> K[🔲 Remove Dark Borders]
    K --> L[⚫ Convert to Grayscale]
    L --> M[🎯 Apply Threshold]
    M --> N[🧹 Noise Reduction]
    N --> O[📏 Upscale Image]
    
    O --> P[🔍 Multiple Preprocessing Methods]
    P --> Q[Method 1: Direct Binary]
    P --> R[Method 2: Otsu Threshold]
    P --> S[Method 3: Adaptive Threshold]
    P --> T[Method 4: Multi-Threshold]
    P --> U[Method 5: Morphology]
    P --> V[Method 6: Inversion]
    
    Q --> W[🔤 Tesseract OCR Processing]
    R --> W
    S --> W
    T --> W
    U --> W
    V --> W
    
    W --> X[📊 Multiple PSM Modes]
    X --> Y[PSM 7: Single Line]
    X --> Z[PSM 8: Single Word]
    X --> AA[PSM 6: Single Block]
    X --> BB[PSM 13: Raw Line]
    
    Y --> CC[✅ Result Validation]
    Z --> CC
    AA --> CC
    BB --> CC
    
    CC --> DD{Valid Result?}
    DD -->|Yes| EE[🎉 Return CAPTCHA Text]
    DD -->|No| FF[❌ Try Next Method]
    FF --> P
    
    EE --> GG[⌨️ Input Text to Game]
    GG --> HH[🖱️ Click Confirm Button]
    HH --> II[⏱️ Wait for Next CAPTCHA]
    II --> E
```

## 🔧 Chi Tiết Các Bước Xử Lý

### 1. **Initialization Phase**
```mermaid
graph LR
    A[Start] --> B[Load config.json]
    B --> C[Initialize Tesseract Engine]
    C --> D[Set OCR Settings]
    D --> E[Select Game Window]
    E --> F[Ready for Processing]
```

### 2. **CAPTCHA Detection & Capture**
```mermaid
graph TD
    A[🔍 Auto-Detect CAPTCHA] --> B{Detection Method}
    B -->|Auto-Detect| C[🎯 Color Analysis]
    B -->|Manual| D[📍 Use Fixed Coordinates]
    B -->|Relative| E[📐 Calculate from Window Size]
    
    C --> F[📊 Confidence Check]
    F --> G{Confidence > 50%?}
    G -->|Yes| H[✅ Use Detected Area]
    G -->|No| I[⚠️ Fallback to Manual]
    
    D --> H
    E --> H
    I --> H
    
    H --> J[📸 Capture Image]
    J --> K[🔍 Analyze Content]
    K --> L[Count Non-White Pixels]
    L --> M{Has Content?}
    M -->|Yes| N[✅ Proceed to Processing]
    M -->|No| O[❌ Retry Capture]
```

### 3. **OpenCV Image Processing Pipeline**
```mermaid
graph TD
    A[📸 Captured Image] --> B[🔲 Remove Dark Borders]
    B --> C[Method 1: Color Detection]
    B --> D[Method 2: Edge Detection]
    
    C --> E[🎨 HSV Color Analysis]
    E --> F[🎭 Create Content Mask]
    F --> G[📐 Find Bounding Rect]
    
    D --> H[⚫ Convert to Grayscale]
    H --> I[🌫️ Gaussian Blur]
    I --> J[🔍 Canny Edge Detection]
    J --> K[📐 Find Contours]
    
    G --> L[✂️ Crop to Content]
    K --> L
    L --> M[📏 Optimize Image Size]
    
    M --> N{Size Check}
    N -->|Too Large| O[📉 Resize Down]
    N -->|Too Small| P[📈 Resize Up]
    N -->|Optimal| Q[✅ Keep Original]
    
    O --> R[🎯 Ready for Preprocessing]
    P --> R
    Q --> R
```

### 4. **Multiple Preprocessing Methods**
```mermaid
graph TD
    A[🖼️ Optimized Image] --> B[🔄 Try Method 1: Direct Binary]
    A --> C[🔄 Try Method 2: Otsu Threshold]
    A --> D[🔄 Try Method 3: Adaptive Threshold]
    A --> E[🔄 Try Method 4: Multi-Threshold]
    A --> F[🔄 Try Method 5: Morphology]
    A --> G[🔄 Try Method 6: Inversion]
    
    B --> H[⚫ Convert to Grayscale]
    H --> I[🎯 Apply Fixed Threshold]
    I --> J[🧹 Morphological Cleanup]
    
    C --> K[⚫ Convert to Grayscale]
    K --> L[🎯 Apply Otsu Threshold]
    L --> M[🧹 Morphological Cleanup]
    
    D --> N[⚫ Convert to Grayscale]
    N --> O[🎯 Apply Adaptive Threshold]
    
    E --> P[⚫ Convert to Grayscale]
    P --> Q[🎯 Try Multiple Thresholds]
    Q --> R[📊 Select Best Result]
    
    F --> S[⚫ Convert to Grayscale]
    S --> T[🎯 Apply Otsu Threshold]
    T --> U[🔧 Morphological Operations]
    U --> V[🧹 Close + Open + Close]
    
    G --> W[⚫ Convert to Grayscale]
    W --> X[🎯 Apply Otsu Threshold]
    X --> Y[🔄 Invert Colors]
    
    J --> Z[🔤 Send to OCR]
    M --> Z
    O --> Z
    R --> Z
    V --> Z
    Y --> Z
```

### 5. **Tesseract OCR Processing**
```mermaid
graph TD
    A[🔤 Preprocessed Image] --> B[🔧 Initialize Tesseract Engine]
    B --> C[⚙️ Set OCR Parameters]
    C --> D[📝 Character Whitelist]
    C --> E[📄 Page Segmentation Mode]
    C --> F[🔍 OCR Engine Mode]
    
    D --> G[🔤 Try PSM 7: Single Line]
    D --> H[🔤 Try PSM 8: Single Word]
    D --> I[🔤 Try PSM 6: Single Block]
    D --> J[🔤 Try PSM 13: Raw Line]
    
    G --> K[📊 Get Confidence Score]
    H --> K
    I --> K
    J --> K
    
    K --> L[📝 Extract Text]
    L --> M[🧹 Clean Result]
    M --> N[✅ Return OCR Result]
```

### 6. **Result Validation & Selection**
```mermaid
graph TD
    A[📊 Multiple OCR Results] --> B[📈 Compare Confidence Scores]
    B --> C[🎯 Select Best Result]
    C --> D[🔍 Validate Result]
    
    D --> E{Length = 4?}
    E -->|No| F[❌ Invalid Length]
    E -->|Yes| G[🔤 All Letters?]
    
    G -->|No| H[❌ Contains Non-Letters]
    G -->|Yes| I[✅ Valid CAPTCHA]
    
    F --> J[🔄 Try Next Method]
    H --> J
    J --> K{More Methods?}
    K -->|Yes| L[🔄 Continue Processing]
    K -->|No| M[❌ Failed to Solve]
    
    I --> N[🎉 Success!]
    L --> A
```

### 7. **Game Automation**
```mermaid
graph TD
    A[✅ Valid CAPTCHA Text] --> B[⌨️ Input Text to Game Field]
    B --> C[⏱️ Wait 500ms]
    C --> D[🖱️ Click Confirm Button]
    D --> E[⏱️ Wait 200ms]
    E --> F[⏱️ Wait 2s for Next CAPTCHA]
    F --> G[🔄 Return to Detection]
    G --> H[🔍 Auto-Detect Next CAPTCHA]
```

## 📊 Performance Metrics

### **Current Status:**
- ✅ **Auto-Detection**: 58.7% confidence
- ✅ **Image Capture**: 200x80 pixels with content
- ✅ **OpenCV Processing**: 8x upscale successful
- ✅ **Border Removal**: Effective dark border detection
- ❌ **OCR Accuracy**: 0.95% confidence (LOW)
- ❌ **Success Rate**: ~0% (needs improvement)

### **Processing Times:**
- 🔍 **ROI Detection**: ~183ms
- 📸 **Image Capture**: ~50ms
- 🖼️ **OpenCV Processing**: ~200ms
- 🔤 **Tesseract OCR**: ~500ms
- ⏱️ **Total**: ~1 second per attempt

## 🎯 Key Issues & Solutions

### **Current Problems:**
1. **Tesseract OCR accuracy too low** (0.95% confidence)
2. **Multiple preprocessing methods not improving results**
3. **Scale factor 8x may be too aggressive**

### **Proposed Solutions:**
1. **Optimize Tesseract settings** - Try different PSM modes
2. **Reduce scale factor** - From 8x to 4x
3. **Improve threshold values** - Test multiple thresholds
4. **Better noise reduction** - Refine morphological operations

## 🔄 Workflow Summary

**Langla-Duky** sử dụng một workflow phức tạp với nhiều phương pháp preprocessing và OCR để giải CAPTCHA:

1. **Detection** → Auto-detect hoặc manual capture
2. **Processing** → OpenCV với 6 phương pháp preprocessing
3. **OCR** → Tesseract với 4 PSM modes
4. **Validation** → Kiểm tra độ dài và format
5. **Automation** → Input text và click button

Workflow hiện tại hoạt động tốt đến 95% nhưng gặp vấn đề ở bước OCR, cần tối ưu để đạt accuracy cao hơn.
