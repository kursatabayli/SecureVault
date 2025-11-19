using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class CreatingChannelState : QrLoginStateBase
{
  private readonly CancellationToken _cancellationToken;

  public CreatingChannelState(ILogger logger, CancellationToken cancellationToken) : base(logger)
  {
    _cancellationToken = cancellationToken;
  }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    Logger.LogInformation("Entering CreatingChannelState: Starting new Hub connection...");
    await context.SetState(QrSessionState.CreatingChannel, "Creating session channel...");

    try
    {
      var result = await context.HubConnection.StartConnectionAsync(null, cancellationToken: _cancellationToken);
      context.ChannelId = result;

      if (string.IsNullOrEmpty(context.ChannelId))
      {
        Logger.LogError("Hub connection started but returned a null or empty Channel ID.");
        await context.HandleErrorAsync("Could not get Channel ID from server.");
        return;
      }

      Logger.LogInformation("Successfully created channel {ChannelId}. Notifying QR availability.", context.ChannelId);
      context.NotifyQrCodeAvailable(context.ChannelId);
      await context.TransitionToAsync(new AwaitingPeerState(Logger, _cancellationToken));
    }
    catch (OperationCanceledException)
    {
      Logger.LogInformation("Session creation canceled by user.");
      await context.TransitionToAsync(new IdleState(Logger));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to start session.");
      await context.HandleErrorAsync($"Failed to start session: {ex.Message}");
    }
  }
}
