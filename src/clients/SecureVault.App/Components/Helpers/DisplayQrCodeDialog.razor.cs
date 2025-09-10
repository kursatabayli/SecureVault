using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services;

namespace SecureVault.App.Components.Helpers
{
    public partial class DisplayQrCodeDialog : ComponentBase
    {
        [Inject] private IQrCodeGenerateService QrCodeGenerateService { get; set; }
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
        [Parameter] public string ChannelId { get; set; }

        private string _qrCodeBase64String = string.Empty;

        protected override void OnInitialized()
        {
            if (!string.IsNullOrWhiteSpace(ChannelId))
                _qrCodeBase64String = QrCodeGenerateService.RenderRoundQrCodeAsBase64(ChannelId);
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
