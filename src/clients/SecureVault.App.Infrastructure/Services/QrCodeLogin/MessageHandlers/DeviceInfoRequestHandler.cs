using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.DTOs.Session;
using System.Text.Json;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.MessageHandlers
{
    public class DeviceInfoRequestHandler : IMessageHandler
    {
        public string MessageType => "DeviceInfoRequest";

        public async Task HandleAsync(IQrLoginContext context, string payload)
        {
            if (context.CurrentRole == QrLoginRole.Provider)
            {
                var deviceInfo = JsonSerializer.Deserialize<DeviceDetailDto>(payload);
                if (deviceInfo != null)
                {
                    await context.RaiseDeviceAuthorizationRequired(deviceInfo);
                }
            }
        }
    }
}
