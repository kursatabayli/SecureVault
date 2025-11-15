using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;

namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

public interface IQrLoginSessionManager : IAsyncDisposable
{
  #region Events (Core -> UI)
  event Action<QrSessionState, string> StateChanged;
  event Action<string> QrCodeAvailable;
  event Action<LoginQrCodeDto> LoginCredentialsReceived;
  event Action<DeviceDetailDto> DeviceAuthorizationRequired;
  event Action AuthorizationComplete;

  #endregion

  #region Commands (UI -> Core)
  Task StartSession(QrLoginRole role, CancellationToken cancellationToken = default);
  Task JoinSessionByScanning(QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken = default);
  Task SendCredentials(bool rememberMeDecision);
  Task ApproveAuthorization();
  Task DenyAuthorization();

  #endregion
}