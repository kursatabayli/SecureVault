using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;

namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

public interface IQrLoginOrchestrator : IAsyncDisposable
{
    event Func<string, Task> OnQrCodeAvailable;
    event Func<QrSessionState, string, Task> OnStateChanged;
    event Func<LoginQrCodeDto, Task> OnLoginCredentialsReceived;
    event Func<Task> OnAuthorizationComplete;

    event Func<DeviceDetailDto, Task> OnDeviceAuthorizationRequired;

    Task StartSession(QrLoginRole role, CancellationToken cancellationToken = default);
    Task JoinSessionByScanning(QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken = default);
    Task SendCredentials(bool rememberMeDecision);
    Task ApproveAuthorization();
    Task DenyAuthorization();
}
