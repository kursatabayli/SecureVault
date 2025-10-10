using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers
{
    public class AuthorizationDeniedHandler : IMessageHandler
    {
        public string MessageType => "AuthorizationDenied";

        public async Task HandleAsync(IQrLoginContext context, string payload)
        {
            if (context.CurrentRole == QrLoginRole.Requester)
            {
                await context.SetState(QrSessionState.Error, "Cihaz bağlantı isteği reddedildi.");
                await context.DisposeAsync();
            }
        }
    }
}
