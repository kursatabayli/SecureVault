using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Services;
using SkiaSharp;
using ZXing.SkiaSharp;

namespace SecureVault.App.Components.Pages.TwoFactorAuthCodes.AddTwoFactorCode
{
    public partial class AddTwoFactorCodeWithQrCodeImage : ComponentBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
        [Inject] private ITwoFactorAuthCodeCreationService CreationService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private ILogger<AddTwoFactorCodeWithQrCodeImage> Logger { get; set; }

        private bool _isProcessing = false;
        private string _statusMessage = "";
        private const long MaxFileSize = 1024 * 1024 * 5;
        private async Task OnFileSelectedAsync(IBrowserFile file)
        {
            if (file == null)
                return;

            if (file.Size > MaxFileSize)
            {
                Snackbar.Add("Dosya boyutu 5 MB'den büyük olamaz.", Severity.Warning);
                return;
            }

            MemoryStream imageMemoryStream;
            try
            {
                imageMemoryStream = new MemoryStream();
                await file.OpenReadStream(MaxFileSize).CopyToAsync(imageMemoryStream);
                imageMemoryStream.Position = 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Dosya okunurken JS Interop hatası oluştu.");
                Snackbar.Add("Dosya okunurken bir hata oluştu. Lütfen tekrar deneyin.", Severity.Error);
                return;
            }

            _isProcessing = true;
            _statusMessage = "Resim işleniyor ve QR kod aranıyor...";
            StateHasChanged();
            await Task.Yield();

            try
            {
                using (imageMemoryStream)
                using (var bitmap = SKBitmap.Decode(imageMemoryStream))
                {
                    if (bitmap == null)
                    {
                        Snackbar.Add("Geçersiz resim formatı veya bozuk dosya.", Severity.Error);
                        return;
                    }

                    var reader = new BarcodeReader();
                    var result = reader.Decode(bitmap);

                    if (result != null && !string.IsNullOrWhiteSpace(result.Text))
                    {
                        await ProcessAndSubmitUriAsync(result.Text);
                    }
                    else
                    {
                        Snackbar.Add("Resimde geçerli bir QR kod bulunamadı.", Severity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "QR kod resmi işlenirken bir hata oluştu.");
                Snackbar.Add("Resim işlenirken beklenmedik bir hata oluştu.", Severity.Error);
            }
            finally
            {
                _isProcessing = false;
                StateHasChanged();
            }
        }

        private async Task ProcessAndSubmitUriAsync(string otpAuthUri)
        {
            if (!CreationService.TryParseOtpAuthUri(otpAuthUri, out var model))
            {
                Snackbar.Add("Geçersiz veya desteklenmeyen QR kod formatı.", Severity.Error);
                MudDialog.Cancel();
                return;
            }

            var success = await CreationService.SubmitCreationCommandAsync(model!, "Resim");
            if (success)
            {
                MudDialog.Close(DialogResult.Ok(model));
            }
            else
            {
                _isProcessing = false;
                StateHasChanged();
            }
        }
        private void Cancel() => MudDialog.Cancel();
    }
}
