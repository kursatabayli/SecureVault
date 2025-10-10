using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers
{
    public class ChannelReadyHandler : IMessageHandler
    {
        public string MessageType => "ChannelReady";

        public async Task HandleAsync(IQrLoginContext context, string payload)
        {
            if (context.CurrentRole == QrLoginRole.Requester)
            {
                await context.SendDeviceInfo();
                await context.SetState(QrSessionState.AwaitingAuthorization, "Onay bekleniyor...");
            }
            else
            {
                await context.SetState(QrSessionState.PeerJoined, "Eşleşme sağlandı. Cihaz bilgisi bekleniyor...");
            }
        }
    }
}
