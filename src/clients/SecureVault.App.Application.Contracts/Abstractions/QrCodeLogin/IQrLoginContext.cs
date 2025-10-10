using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;

namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin
{
    public interface IQrLoginContext
    {
        QrLoginRole CurrentRole { get; }
        bool IsPublicKeySent { get; }
        ISecureChannelManager SecureChannelManager { get; }

        Task SetState(QrSessionState newState, string message);
        Task InitiateKeyExchange();
        Task SendDeviceInfo();
        ValueTask DisposeAsync();

        Task RaiseDeviceAuthorizationRequired(DeviceDetailDto deviceInfo);
        Task RaiseLoginCredentialsReceived(LoginQrCodeDto credentials);
    }
}
