using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers
{
    public class AuthorizationApprovedHandler : IMessageHandler
    {
        public string MessageType => "AuthorizationApproved";

        public async Task HandleAsync(IQrLoginContext context, string payload)
        {
            if (context.CurrentRole == QrLoginRole.Requester)
            {
                await context.InitiateKeyExchange();
            }
        }
    }
}
