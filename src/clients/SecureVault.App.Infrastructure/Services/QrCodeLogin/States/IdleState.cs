using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class IdleState : QrLoginStateBase
{
  public IdleState(ILogger logger) : base(logger) { }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    Logger.LogInformation("Entering IdleState: Session context is being reset.");

    context.IsPublicKeySent = false;
    context.ChannelId = null;

    await context.SetState(QrSessionState.Idle, "Session idle", true);
  }

  public override async Task StartSessionAsync(QrLoginSessionManager context, QrLoginRole role, CancellationToken cancellationToken)
  {
    Logger.LogInformation("IdleState: StartSessionAsync called. Role: {Role}", role);
    context.CurrentRole = role;
    await context.TransitionToAsync(new CreatingChannelState(Logger, cancellationToken));
  }

  public override async Task JoinSessionByScanningAsync(QrLoginSessionManager context, QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken)
  {
    Logger.LogInformation("IdleState: JoinSessionByScanningAsync called. Role: {Role}", role);

    if (string.IsNullOrEmpty(scannedChannelId))
    {
      await context.HandleErrorAsync("Scanned Channel ID cannot be empty.");
      return;
    }

    context.CurrentRole = role;
    context.ChannelId = scannedChannelId;
    await context.TransitionToAsync(new JoiningChannelState(Logger, cancellationToken));
  }
}