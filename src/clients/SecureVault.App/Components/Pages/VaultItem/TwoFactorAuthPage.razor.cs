using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OtpNet;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Components.Pages.VaultItem
{
    public partial class TwoFactorAuthPage : ComponentBase, IDisposable
    {
        [Inject] private ITotpService TotpService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        // Veri artık doğrudan servisten okunuyor.
        private IReadOnlyList<TotpViewModel> DisplayItems => TotpService.Items;

        protected override async Task OnInitializedAsync()
        {
            TotpService.OnTick += OnTotpTick;
            await TotpService.InitializeAsync();
        }

        private void OnTotpTick()
        {
            InvokeAsync(StateHasChanged);
        }

        private async Task CopyCodeToClipboard(TotpViewModel item)
        {
            if (item.CurrentCode != "------" && item.CurrentCode != "HATA!")
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", item.CurrentCode);
                Snackbar.Add($"'{item.Model.Issuer}' için kod panoya kopyalandı!", Severity.Success, config => { config.VisibleStateDuration = 2000; });
            }
        }

        public void Dispose()
        {
            TotpService.OnTick -= OnTotpTick;
        }
    }
}
