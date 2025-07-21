using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service;

namespace SecureVault.App.Components.Pages.VaultItem
{
    public partial class TwoFactorAuthPage : ComponentBase, IDisposable
    {
        [Inject] private IOtpService OtpService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;

        private IReadOnlyList<OtpViewModel> DisplayItems => OtpService.Items;

        protected override async Task OnInitializedAsync()
        {
            OtpService.OnTick += OnTotpTick;
            await OtpService.InitializeAsync();
        }

        private void OnTotpTick()
        {
            InvokeAsync(StateHasChanged);
        }
        private async Task HandleItemClick(OtpViewModel item)
        {
            if (item.Model.Type == OtpType.HOTP && !item.IsCodeReady)
            {
                await OtpService.GenerateHotpCodeAsync(item.Model.Id);
            }
            else if (item.Model.Type == OtpType.HOTP && item.IsCodeReady)
            {
                await OtpService.GenerateHotpCodeAsync(item.Model.Id);
            }
            else if (item.Model.Type == OtpType.TOTP && item.IsCodeReady)
            {
                await CopyCodeToClipboard(item);
            }
        }
        private async Task CopyCodeToClipboard(OtpViewModel item)
        {
            if (item.IsCodeReady)
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", item.CurrentCode);
                Snackbar.Add(Localizer[SharedResources.Copied], Severity.Success, config => { config.VisibleStateDuration = 2000; });
            }
        }

        public void Dispose()
        {
            OtpService.OnTick -= OnTotpTick;
        }
    }
}
