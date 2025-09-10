using QRCoder;
using SkiaSharp;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.SkiaSharp;

namespace SecureVault.App.Services
{
    public class QrCodeGenerateService : IQrCodeGenerateService
    {
        public string GenerateQrCodeAsBase64(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentException("QR koda dönüştürülecek metin boş olamaz.", nameof(plainText));
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(plainText, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);

            using var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsBytes = pngQrCode.GetGraphic(20);
            string base64String = Convert.ToBase64String(qrCodeAsBytes);
            return $"data:image/png;base64,{base64String}";
        }

        public string GenerateQrCodeAsSvg(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(plainText, QRCodeGenerator.ECCLevel.Q, forceUtf8: true);

            using var qrCode = new SvgQRCode(qrCodeData);
            string svgImage = qrCode.GetGraphic(20);

            return svgImage;
        }
        public string GenerateQrCodeWithZXingAsBase64(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

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
            return $"data:image/png;base64,{base64String}";
        }
        public string RenderRoundQrCodeAsBase64(string plainText)
        {

            //string logoFilePath = "appicon.ico";
            //byte[] logoBytes = null;

            //if (File.Exists(logoFilePath))
            //{
            //    logoBytes = File.ReadAllBytes(logoFilePath);
            //}
            byte[] qrCodeBytes = RenderRoundQrCode(plainText, 30);

            if (qrCodeBytes == null)
            {
                string fallBackLine = GenerateQrCodeAsBase64(plainText);
                if (!string.IsNullOrEmpty(fallBackLine))
                    return fallBackLine;
                return string.Empty;
            }

            string base64String = Convert.ToBase64String(qrCodeBytes);
            return $"data:image/png;base64,{base64String}";
        }

        private byte[] RenderRoundQrCode(string data, int pixelsPerModule, byte[] logoBytes = null)
        {
            using SKBitmap qrBitmap = GenerateRoundQrBitmap(data, pixelsPerModule, ErrorCorrectionLevel.H);

            if (qrBitmap == null)
                return null;

            if (logoBytes == null || logoBytes.Length == 0)
            {
                using var plainImage = SKImage.FromBitmap(qrBitmap);
                using var dataStream = plainImage.Encode(SKEncodedImageFormat.Png, 100);
                return dataStream.ToArray();
            }

            using var canvas = new SKCanvas(qrBitmap);
            using var logoBitmap = SKBitmap.Decode(logoBytes);

            int logoMaxSize = qrBitmap.Width / 4;

            using var scaledLogo = logoBitmap.Resize(new SKImageInfo(logoMaxSize, logoMaxSize, SKColorType.Bgra8888, SKAlphaType.Premul), SKSamplingOptions.Default);

            float backplateSize = scaledLogo.Width + (pixelsPerModule);
            float backplateX = (qrBitmap.Width - backplateSize) / 2f;
            float backplateY = (qrBitmap.Height - backplateSize) / 2f;

            using var backplatePaint = new SKPaint { Color = SKColors.White, BlendMode = SKBlendMode.Src, IsAntialias = true };

            canvas.DrawRoundRect(backplateX, backplateY, backplateSize, backplateSize, pixelsPerModule, pixelsPerModule, backplatePaint);

            float logoX = (qrBitmap.Width - scaledLogo.Width) / 2f;
            float logoY = (qrBitmap.Height - scaledLogo.Height) / 2f;
            canvas.DrawBitmap(scaledLogo, logoX, logoY);

            using var finalImage = SKImage.FromBitmap(qrBitmap);
            using var finalDataStream = finalImage.Encode(SKEncodedImageFormat.Png, 100);
            return finalDataStream.ToArray();
        }

        private SKBitmap GenerateRoundQrBitmap(string data, int pixelsPerModule, ErrorCorrectionLevel eccLevel)
        {
            try
            {
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
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"QR kod bitmap'i oluşturulurken hata oluştu: {ex.Message}");
                return null;
            }
        }
    }
}
