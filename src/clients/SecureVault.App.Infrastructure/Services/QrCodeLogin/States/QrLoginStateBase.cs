
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public abstract class QrLoginStateBase : IQrLoginState
{
  protected readonly ILogger Logger;

  protected QrLoginStateBase(ILogger logger)
  {
    Logger = logger;
  }

  public virtual Task HandleEnterAsync(QrLoginSessionManager context)
  {
    return Task.CompletedTask;
  }

  public virtual Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload)
  {
    Logger.LogWarning("State {State} received unexpected message: {MessageType}",
        GetType().Name, messageType);
    return Task.CompletedTask;
  }

  public virtual Task StartSessionAsync(QrLoginSessionManager context, QrLoginRole role, CancellationToken cancellationToken)
  {
    Logger.LogWarning("StartSessionAsync called in unexpected state: {State}", GetType().Name);
    return Task.CompletedTask;
  }

  public virtual Task JoinSessionByScanningAsync(QrLoginSessionManager context, QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken)
  {
    Logger.LogWarning("JoinSessionByScanningAsync called in unexpected state: {State}", GetType().Name);
    return Task.CompletedTask;
  }

  public virtual Task ApproveAuthorizationAsync(QrLoginSessionManager context)
  {
    Logger.LogWarning("ApproveAuthorizationAsync called in unexpected state: {State}", GetType().Name);
    return Task.CompletedTask;
  }

  public virtual Task DenyAuthorizationAsync(QrLoginSessionManager context)
  {
    Logger.LogWarning("DenyAuthorizationAsync called in unexpected state: {State}", GetType().Name);
    return Task.CompletedTask;
  }

  public virtual Task SendCredentialsAsync(QrLoginSessionManager context, LoginQrCodeDto credentials)
  {
    Logger.LogWarning("SendCredentialsAsync called in unexpected state: {State}", GetType().Name);
    return Task.CompletedTask;
  }

  public virtual Task HandleQrRefreshedAsync(QrLoginSessionManager context, string newChannelId)
  {
    Logger.LogWarning("HandleQrRefreshedAsync called in unexpected state: {State}", GetType().Name);
    return Task.CompletedTask;
  }

  public virtual Task HandleErrorAsync(QrLoginSessionManager context, string errorMessage)
  {
    Logger.LogError("HandleErrorAsync called: {ErrorMessage}", errorMessage);
    return context.SetState(QrSessionState.Error, errorMessage, true);
  }
}