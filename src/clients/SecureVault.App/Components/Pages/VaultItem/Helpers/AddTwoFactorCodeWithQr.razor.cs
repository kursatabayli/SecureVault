using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Services;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Contracts;
using System.Web;

namespace SecureVault.App.Components.Pages.VaultItem.Helpers
{
    public partial class AddTwoFactorCodeWithQr : ComponentBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
        [Inject] IQrCodeScannerService QrScannerService { get; set; }
        [Inject] IVaultItemService<TwoFactorAuthModel> VaultItemService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] ILogger<AddTwoFactorCodeWithQr> Logger { get; set; }
        [Inject] IStringLocalizer<SharedResources> Localizer { get; set; }

        private TwoFactorAuthModel _twoFactorAuthModel = new();

        private bool _isSubmitting = false;


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ScanProcessAndSubmitAsync();
            }
        }

        private async Task ScanProcessAndSubmitAsync()
        {
            var otpAuthUri = await QrScannerService.ScanAsync();

            if (string.IsNullOrWhiteSpace(otpAuthUri))
            {
                MudDialog.Cancel();
                return;
            }

            if (!ParseOtpAuthUri(otpAuthUri))
            {
                Snackbar.Add("Geçersiz veya desteklenmeyen QR kod formatı.", Severity.Error);
                MudDialog.Cancel();
                return;
            }

            _isSubmitting = true;
            StateHasChanged();

            try
            {
                var result = await VaultItemService.CreateVaultItem(_twoFactorAuthModel, ItemType.TwoFactorAuth);

                if (result.IsSuccess)
                {
                    Snackbar.Add(Localizer[SharedResources.TwoFactorAddedSuccessfully], Severity.Success);
                    MudDialog.Close(DialogResult.Ok(_twoFactorAuthModel));
                }
                else
                {
                    Snackbar.Add(result.Error.Message, Severity.Error);
                    MudDialog.Cancel();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "2FA kodu (QR) ekleme sırasında beklenmedik bir hata oluştu.");
                Snackbar.Add(Localizer[SharedResources.UnexpectedError], Severity.Error);
                MudDialog.Cancel();
            }
        }

        private bool ParseOtpAuthUri(string otpAuthUri)
        {
            try
            {
                if (!otpAuthUri.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
                    return false;

                var uri = new Uri(otpAuthUri);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);

                _twoFactorAuthModel.Type = uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase) ? OtpType.TOTP : OtpType.HOTP;

                var secret = queryParams["secret"];
                if (string.IsNullOrEmpty(secret)) return false;
                _twoFactorAuthModel.SecretKey = secret;

                var path = uri.AbsolutePath.TrimStart('/');
                var issuerFromQuery = queryParams["issuer"];
                if (!string.IsNullOrEmpty(issuerFromQuery))
                {
                    _twoFactorAuthModel.Issuer = issuerFromQuery;
                    _twoFactorAuthModel.AccountName = path.StartsWith(issuerFromQuery)
                        ? path.Substring(issuerFromQuery.Length).TrimStart(':')
                        : path;
                }
                else
                {
                    var pathParts = path.Split(new[] { ':' }, 2);
                    if (pathParts.Length > 1)
                    {
                        _twoFactorAuthModel.Issuer = pathParts[0];
                        _twoFactorAuthModel.AccountName = pathParts[1];
                    }
                    else
                    {
                        _twoFactorAuthModel.AccountName = path;
                    }
                }

                if (int.TryParse(queryParams["digits"], out var digits)) _twoFactorAuthModel.Digits = digits;
                if (int.TryParse(queryParams["period"], out var period)) _twoFactorAuthModel.Period = period;
                if (long.TryParse(queryParams["counter"], out var counter)) _twoFactorAuthModel.Counter = counter;

                var algorithmStr = queryParams["algorithm"];
                if (!string.IsNullOrEmpty(algorithmStr))
                    if (Enum.TryParse<OtpAlgorithm>(algorithmStr, true, out var algorithm))
                        _twoFactorAuthModel.Algorithm = algorithm;

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OTP URI ayrıştırılırken hata oluştu: {Uri}", otpAuthUri);
                return false;
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
