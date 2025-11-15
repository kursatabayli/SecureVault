using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public interface IQrLoginState
{
  Task HandleEnterAsync(QrLoginSessionManager context);
  Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload);
  Task StartSessionAsync(QrLoginSessionManager context, QrLoginRole role, CancellationToken cancellationToken);
  Task JoinSessionByScanningAsync(QrLoginSessionManager context, QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken);
  Task ApproveAuthorizationAsync(QrLoginSessionManager context);
  Task DenyAuthorizationAsync(QrLoginSessionManager context);
  Task SendCredentialsAsync(QrLoginSessionManager context, LoginQrCodeDto credentials);
  Task HandleQrRefreshedAsync(QrLoginSessionManager context, string newChannelId);
  Task HandleErrorAsync(QrLoginSessionManager context, string errorMessage);
}
