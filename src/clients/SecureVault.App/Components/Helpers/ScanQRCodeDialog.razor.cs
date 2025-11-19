using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services.Interfaces;

namespace SecureVault.App.Components.Helpers;

public partial class ScanQRCodeDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
    [Inject] IQrCodeScannerService QrScannerService { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await ScanAndCloseAsync();
    }

    private async Task ScanAndCloseAsync()
    {
        try
        {
            string qrCodeText = await QrScannerService.ScanAsync();

            if (!string.IsNullOrEmpty(qrCodeText))
            {
                MudDialog.Close(DialogResult.Ok(qrCodeText));
            }
            else
            {
                MudDialog.Cancel();
            }
        }
        catch (Exception)
        {
            MudDialog.Cancel();
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
