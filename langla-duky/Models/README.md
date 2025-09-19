# Model: Captcha4DigitProcessor

## Overview

`Captcha4DigitProcessor` is a specialized class designed to solve 4-digit numerical captchas. It follows a simple and efficient workflow using OpenCV for image preprocessing and Tesseract for optical character recognition (OCR).

## Workflow

1.  **Input**: A `Bitmap` image of the captcha.
2.  **Grayscale Conversion**: The image is converted to grayscale.
3.  **Binarization**: An Otsu threshold is applied to create a black-and-white (binary) image, which is ideal for OCR.
4.  **Upscaling**: The binary image is upscaled by a factor of 2 to improve OCR accuracy on small text.
5.  **Tesseract OCR**: The processed image is passed to the Tesseract engine, which is configured to recognize **only digits (0-9)**.
6.  **Validation**: The OCR result is cleaned and validated to ensure it is exactly 4 digits long.
7.  **Output**:
    *   If validation succeeds, a `string` containing the 4 digits is returned.
    *   If the process fails at any step or validation fails, an empty `string` is returned.

## Usage

Instantiate the processor and call the `ProcessCaptcha4Digits` method.

```csharp
using langla_duky.Models;
using System.Drawing;

// The constructor can optionally take a path to the Tesseract data files.
var processor = new Captcha4DigitProcessor("./tessdata");

using var captchaImage = new Bitmap("path/to/your/captcha.png");

string result = processor.ProcessCaptcha4Digits(captchaImage);

if (!string.IsNullOrEmpty(result))
{
    Console.WriteLine($"Captcha solved: {result}");
}
else
{
    Console.WriteLine("Failed to solve captcha.");
}
```

## Configuration

The behavior is implicitly controlled by the `Use4DigitMode` flag in `config.json`. When this mode is active, `MainForm` delegates captcha solving to this processor.
