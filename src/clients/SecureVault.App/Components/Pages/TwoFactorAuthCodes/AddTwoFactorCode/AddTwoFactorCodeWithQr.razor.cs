using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services.Interfaces;

namespace SecureVault.App.Components.Pages.TwoFactorAuthCodes.AddTwoFactorCode;

public partial class AddTwoFactorCodeWithQr : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
    [Inject] private IQrCodeScannerService QrScannerService { get; set; }
    [Inject] private ITwoFactorAuthCodeCreationService CreationService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; }

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

        if (!CreationService.TryParseOtpAuthUri(otpAuthUri, out var model))
        {
            Snackbar.Add("Geçersiz veya desteklenmeyen QR kod formatı.", Severity.Error);
            MudDialog.Cancel();
            return;
        }

        _isSubmitting = true;
        StateHasChanged();

        var success = await CreationService.SubmitCreationCommandAsync(model!, "QR Kamera");
        if (success)
        {
            MudDialog.Close(DialogResult.Ok(model));
        }
        else
        {
            MudDialog.Cancel();
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
