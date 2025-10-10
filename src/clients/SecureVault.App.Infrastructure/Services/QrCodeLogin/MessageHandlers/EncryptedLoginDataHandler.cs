using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers
{
    public class EncryptedLoginDataHandler : IMessageHandler
    {
        private readonly ILogger<EncryptedLoginDataHandler> _logger;
        public string MessageType => "EncryptedLoginData";

        public EncryptedLoginDataHandler(ILogger<EncryptedLoginDataHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleAsync(IQrLoginContext context, string payload)
        {
            if (context.CurrentRole == QrLoginRole.Requester)
            {
                await context.SetState(QrSessionState.TransferringCredentials, "Oturum bilgileri alındı, doğrulanıyor...");
                try
                {
                    var encryptedBytes = Convert.FromBase64String(payload);
                    var decryptedDto = context.SecureChannelManager.Decrypt<LoginQrCodeDto>(encryptedBytes);
                    await context.RaiseLoginCredentialsReceived(decryptedDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Oturum bilgileri çözülemedi.");
                    await context.SetState(QrSessionState.Error, $"Oturum bilgileri çözülemedi: {ex.Message}");
                }
            }
        }
    }
}
