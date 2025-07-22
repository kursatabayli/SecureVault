using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SecureVault.App.Components.Pages.VaultItem.Helpers
{
    public partial class AddTwoFactorAuthFabMenu : ComponentBase
    {
        [Parameter] public EventCallback OnItemAdded { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        private bool isFabMenuOpen = false;
        private void ToggleFabMenu()
        {
            isFabMenuOpen = !isFabMenuOpen;
        }
        private async Task ScanQrCode()
        {
            var dialog = await DialogService.ShowAsync<AddTwoFactorCodeWithQr>();
            var result = await dialog.Result;
            if (!result.Canceled)
                await OnItemAdded.InvokeAsync();

            isFabMenuOpen = false;
        }
        private async Task ManualEntry()
        {
            var dialog = await DialogService.ShowAsync<AddTwoFactorCodeManuel>();
            var result = await dialog.Result;
            if (!result.Canceled)
                await OnItemAdded.InvokeAsync();

            isFabMenuOpen = false;
        }
    }
}
