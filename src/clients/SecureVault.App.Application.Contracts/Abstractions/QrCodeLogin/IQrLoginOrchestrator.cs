using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin
{
    public enum QrLoginRole
    {
        Provider,
        Requester
    }

    public enum InitiationMethod
    {
        GenerateQrCode,
        ScanQrCode
    }

    public interface IQrLoginOrchestrator : IAsyncDisposable
    {
        event Func<string, Task> OnQrCodeAvailable;
        event Func<string, Task> OnStatusUpdate;
        event Func<LoginQrCodeDto, Task> OnLoginCredentialsReceived;
        event Func<Task> OnAuthorizationComplete;
        event Func<string, Task> OnError;
        Task StartSession(QrLoginRole role, InitiationMethod method, string? scannedChannelId = null, CancellationToken cancellationToken = default);
        Task SendCredentials(LoginQrCodeDto credentials);
    }
}
