using Microsoft.Extensions.Logging;
using QRCoder;
using SecureVault.App.Services.Interfaces;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.SkiaSharp;

namespace SecureVault.App.Services.Implementations;

public class QrCodeGenerateService : IQrCodeGenerateService
{
    private readonly ILogger<QrCodeGenerateService> _logger;
    public QrCodeGenerateService(ILogger<QrCodeGenerateService> logger)
    {
        _logger = logger;
    }
    public string GenerateQrCodeAsBase64(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            _logger.LogError("QR code generation failed: Plain text input is null or empty.");
            throw new ArgumentException("The text to be converted to QR code cannot be empty.", nameof(plainText));
        }

        _logger.LogDebug("Generating Base64 PNG QR code using QRCoder.");

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(plainText, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);

        using var pngQrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeAsBytes = pngQrCode.GetGraphic(20);
        string base64String = Convert.ToBase64String(qrCodeAsBytes);

        _logger.LogDebug("Base64 QR code successfully generated and encoded.");
        return $"data:image/png;base64,{base64String}";
    }

    public string GenerateQrCodeAsSvg(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        _logger.LogDebug("Generating SVG QR code using QRCoder.");

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(plainText, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);

        using var qrCode = new SvgQRCode(qrCodeData);
        string svgImage = qrCode.GetGraphic(20);

        _logger.LogDebug("SVG QR code successfully generated.");
        return svgImage;
    }

    public string GenerateQrCodeWithZXingAsBase64(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        _logger.LogDebug("Generating Base64 PNG QR code using ZXing.SkiaSharp.");

        try
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = 300,
                    Height = 300,
                    Margin = 0,
                    ErrorCorrection = ErrorCorrectionLevel.L
                }
            };

            using var skBitmap = writer.Write(plainText);
            using var memoryStream = new MemoryStream();
            skBitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
            byte[] imageBytes = memoryStream.ToArray();
            string base64String = Convert.ToBase64String(imageBytes);

            _logger.LogDebug("ZXing QR code successfully generated and encoded.");
            return $"data:image/png;base64,{base64String}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code using ZXing.SkiaSharp.");
            return string.Empty;
        }
    }

    public string RenderRoundQrCodeAsBase64(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        _logger.LogInformation("Attempting to render rounded QR code with custom bitmap logic.");

        byte[] qrCodeBytes = RenderRoundQrCode(plainText, 30);

        if (qrCodeBytes == null)
        {
            _logger.LogWarning("Rounded QR code rendering failed. Falling back to simple QRCoder PNG.");
            string fallBackLine = GenerateQrCodeAsBase64(plainText);

            if (!string.IsNullOrEmpty(fallBackLine))
                return fallBackLine;

            _logger.LogError("Fallback QR code generation also failed.");
            return string.Empty;
        }

        string base64String = Convert.ToBase64String(qrCodeBytes);
        _logger.LogInformation("Rounded QR code successfully rendered.");
        return $"data:image/png;base64,{base64String}";
    }

    private byte[] RenderRoundQrCode(string data, int pixelsPerModule, byte[]? logoBytes = null)
    {
        if (logoBytes != null && logoBytes.Length > 0)
        {
            _logger.LogWarning("Logo rendering logic is deprecated/not implemented here and was skipped.");
        }

        using SKBitmap qrBitmap = GenerateRoundQrBitmap(data, pixelsPerModule, ErrorCorrectionLevel.H);

        if (qrBitmap == null)
            return null;

        using var finalImage = SKImage.FromBitmap(qrBitmap);
        using var finalDataStream = finalImage.Encode(SKEncodedImageFormat.Png, 100);
        return finalDataStream.ToArray();
    }

    private SKBitmap GenerateRoundQrBitmap(string data, int pixelsPerModule, ErrorCorrectionLevel eccLevel)
    {
        try
        {
            _logger.LogDebug("Generating rounded QR bitmap with ZXing and SkiaSharp...");

            var qrWriter = new QRCodeWriter();
            var hints = new Dictionary<EncodeHintType, object>
            {
                { EncodeHintType.ERROR_CORRECTION, eccLevel },
                { EncodeHintType.MARGIN, 0 },
                { EncodeHintType.CHARACTER_SET, "UTF-8" }
            };

            var bitMatrix = qrWriter.encode(data, BarcodeFormat.QR_CODE, 0, 0, hints);

            int qrSize = bitMatrix.Width;
            int imageSize = qrSize * pixelsPerModule;
            float radius = (float)pixelsPerModule / 2;

            var bitmap = new SKBitmap(imageSize, imageSize);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            using var darkPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };

            for (int y = 0; y < qrSize; y++)
            {
                for (int x = 0; x < qrSize; x++)
                {
                    if (!bitMatrix[x, y]) continue;
                    float centerX = x * pixelsPerModule + radius;
                    float centerY = y * pixelsPerModule + radius;
                    canvas.DrawCircle(centerX, centerY, radius, darkPaint);

                    if (x + 1 < qrSize && bitMatrix[x + 1, y])
                    {
                        var hRect = SKRect.Create(centerX, centerY - radius, pixelsPerModule, pixelsPerModule);
                        canvas.DrawRect(hRect, darkPaint);
                    }
                    if (y + 1 < qrSize && bitMatrix[x, y + 1])
                    {
                        var vRect = SKRect.Create(centerX - radius, centerY, pixelsPerModule, pixelsPerModule);
                        canvas.DrawRect(vRect, darkPaint);
                    }
                }
            }
            _logger.LogDebug("Rounded QR bitmap generation complete.");
            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate rounded QR code bitmap.");
            return null;
        }
    }
}
