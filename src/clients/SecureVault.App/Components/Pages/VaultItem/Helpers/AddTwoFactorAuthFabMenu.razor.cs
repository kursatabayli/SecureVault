using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SecureVault.App.Components.Pages.VaultItem.Helpers
{
    public partial class AddTwoFactorAuthFabMenu : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; }
        private bool isFabMenuOpen = false;
        private void ToggleFabMenu()
        {
            isFabMenuOpen = !isFabMenuOpen;
        }
        private void ScanQrCode()
        {
            Snackbar.Add("QR Kod okuyucu burada açılacak.", Severity.Info);
            isFabMenuOpen = false;
        }
        private async Task ManualEntry()
        {
            var parameters = new DialogParameters<AddTwoFactorCodeManuel>();
            var dialog = await DialogService.ShowAsync<AddTwoFactorCodeManuel>(null, parameters);
            var result = await dialog.Result;
            await OnInitializedAsync();
            isFabMenuOpen = false;
        }
    }
}
