using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.App.Resources.Localization;
using SecureVault.App.Services.Interfaces;
using Color = MudBlazor.Color;

namespace SecureVault.App.Components.Pages.TwoFactorAuthCodes;

public partial class TwoFactorAuthCodes : ComponentBase, IDisposable
{
    [Inject] private IOtpService OtpService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;
    private string searchString = "";
    private IEnumerable<OtpViewModel> FilteredAndSortedItems
    {
        get
        {
            var items = OtpService.Items;
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                items = [.. items.Where(item =>
                    item.Model.Issuer.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    item.Model.AccountName.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                )];
            }
            return items.OrderBy(item => item.Model.Issuer).ThenBy(item => item.Model.AccountName);
        }
    }
    protected override async Task OnInitializedAsync()
    {
        OtpService.OnTick += OnTotpTick;
        await OtpService.InitializeAsync();
    }

    private async Task OnTotpTick() => await InvokeAsync(StateHasChanged);
    private async Task HandleItemClick(OtpViewModel item) => await OtpService.GenerateHotpCodeAsync(item.Model.Id);
    private async Task CopyCodeToClipboard(OtpViewModel item)
    {
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", item.CurrentCode);
        Snackbar.Add(Localizer[SharedResources.Copied], Severity.Success, config => { config.VisibleStateDuration = 2000; });
    }
    private Color GetProgressColor(int timeLeft) => timeLeft <= 5 ? Color.Error : Color.Primary;
    private string FormatCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != 6)
            return code;

        return $"{code.Substring(0, 3)} {code.Substring(3, 3)}";
    }

    public void Dispose()
    {
        OtpService.OnTick -= OnTotpTick;
    }
}
