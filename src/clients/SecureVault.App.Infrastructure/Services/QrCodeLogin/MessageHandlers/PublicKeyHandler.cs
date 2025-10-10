using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers
{
    public class PublicKeyHandler : IMessageHandler
    {
        public string MessageType => "PublicKey";

        public async Task HandleAsync(IQrLoginContext context, string payload)
        {
            if (!context.IsPublicKeySent)
            {
                await context.InitiateKeyExchange();
            }
            context.SecureChannelManager.FinalizeKeyExchange(payload);
            await context.SetState(QrSessionState.SecureChannelEstablished, "Güvenli kanal kuruldu.");
        }
    }
}
