using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class JoiningChannelState : QrLoginStateBase
{
  private readonly CancellationToken _cancellationToken;

  public JoiningChannelState(ILogger logger, CancellationToken cancellationToken) : base(logger)
  {
    _cancellationToken = cancellationToken;
  }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    Logger.LogInformation("Entering JoiningChannelState: Attempting to join ChannelId: {ChannelId}", context.ChannelId);

    await context.SetState(QrSessionState.CreatingChannel, "Joining session...");

    try
    {
      await context.HubConnection.StartConnectionAsync(context.ChannelId, _cancellationToken);

      Logger.LogInformation("Successfully joined channel {ChannelId}. Transitioning to AwaitingPeerState.", context.ChannelId);

      await context.TransitionToAsync(new AwaitingPeerState(Logger, _cancellationToken));
    }
    catch (OperationCanceledException)
    {
      Logger.LogInformation("Session joining was canceled by the user (ChannelId: {ChannelId}).", context.ChannelId);
      await context.TransitionToAsync(new IdleState(Logger));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to join session (ChannelId: {ChannelId}).", context.ChannelId);
      await context.HandleErrorAsync($"Failed to join session: {ex.Message}");
    }
  }
}