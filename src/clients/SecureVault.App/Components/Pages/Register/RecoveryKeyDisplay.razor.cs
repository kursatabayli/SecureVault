using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SecureVault.App.Services.Models.RecoveryKeyModels;

namespace SecureVault.App.Components.Pages.Register
{
    public partial class RecoveryKeyDisplay : ComponentBase
    {
        private bool _isCopied = false;
        private string _buttonAnimationClass = "";
        private bool _recoveryKeyConfirmed = false;
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        [Parameter] public RecoveryKeyModel RecoveryKey { get; set; } = null!;
        [Parameter] public EventCallback OnSubmit { get; set; }
        [Parameter] public EventCallback OnBack { get; set; }

        private async Task HandleSubmit()
        {
            if (OnSubmit.HasDelegate)
            {
                await OnSubmit.InvokeAsync();
            }
        }
        private async Task HandleBack()
        {
            if (OnBack.HasDelegate)
            {
                await OnBack.InvokeAsync();
            }
        }
        private async Task CopyToClipboard()
        {
            if (_isCopied || RecoveryKey is null) return;
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", RecoveryKey.Mnemonic);
            _isCopied = true;
            _buttonAnimationClass = "copied-pop-in-animation";
            StateHasChanged();

            await Task.Delay(2000);

            _buttonAnimationClass = "copied-pop-out-animation";
            StateHasChanged();

            await Task.Delay(300);

            _isCopied = false;
            StateHasChanged();
        }
    }
}
