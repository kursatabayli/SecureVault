using Microsoft.AspNetCore.Components;
using SecureVault.App.Services;

namespace SecureVault.App.Components.Helpers
{
    public partial class DisplayQrCode : ComponentBase
    {
        [Inject] private IQrCodeGenerateService QrCodeGenerateService { get; set; }

        [Parameter] public string ChannelId { get; set; }

        [Parameter] public EventCallback OnClose { get; set; }

        private string _qrCodeBase64String = string.Empty;

        protected override void OnInitialized()
        {
            if (!string.IsNullOrWhiteSpace(ChannelId))
            {
                _qrCodeBase64String = QrCodeGenerateService.RenderRoundQrCodeAsBase64(ChannelId);
            }
        }

        protected override void OnParametersSet()
        {
            if (!string.IsNullOrWhiteSpace(ChannelId))
            {
                var newQrCode = QrCodeGenerateService.RenderRoundQrCodeAsBase64(ChannelId);
                if (newQrCode != _qrCodeBase64String)
                {
                    _qrCodeBase64String = newQrCode;
                    StateHasChanged();
                }
            }
        }

        private async Task HandleClose()
        {
            await OnClose.InvokeAsync();
        }
    }
}
