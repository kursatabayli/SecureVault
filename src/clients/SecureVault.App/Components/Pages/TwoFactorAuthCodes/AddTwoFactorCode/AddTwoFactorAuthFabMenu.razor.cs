using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SecureVault.App.Components.Pages.TwoFactorAuthCodes.AddTwoFactorCode;

public partial class AddTwoFactorAuthFabMenu : ComponentBase
{
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    private bool isFabMenuOpen = false;
    private void ToggleFabMenu()
    {
        isFabMenuOpen = !isFabMenuOpen;
    }
    private async Task ScanQrCode()
    {
        var dialog = await DialogService.ShowAsync<AddTwoFactorCodeWithQr>();
        var result = await dialog.Result;
        if (!result.Canceled && result.Data is bool success && success)
            Snackbar.Add("İki faktörlü kimlik doğrulama kodu başarıyla eklendi.", Severity.Success);

        isFabMenuOpen = false;
    }
    private async Task ManualEntry()
    {
        var dialog = await DialogService.ShowAsync<AddTwoFactorCodeManuel>();
        var result = await dialog.Result;
        if (!result.Canceled && result.Data is bool success && success)
            Snackbar.Add("İki faktörlü kimlik doğrulama kodu başarıyla eklendi.", Severity.Success);

        isFabMenuOpen = false;
    }

    private async Task OpenAddWithImageDialog()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<AddTwoFactorCodeWithQrCodeImage>("Resimden QR Kod ile Ekle", options);
        var result = await dialog.Result;
        if (!result.Canceled && result.Data is bool success && success)
            Snackbar.Add("İki faktörlü kimlik doğrulama kodu başarıyla eklendi.", Severity.Success);

        isFabMenuOpen = false;
    }

}
