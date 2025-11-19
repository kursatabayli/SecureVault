using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class AwaitingPeerState : QrLoginStateBase
{
  private const int QrRefreshIntervalSeconds = 55;
  private readonly CancellationToken _cancellationToken;

  public AwaitingPeerState(ILogger logger, CancellationToken cancellationToken) : base(logger)
  {
    _cancellationToken = cancellationToken;
  }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    Logger.LogInformation("Entering AwaitingPeerState: Starting QR code refresher (Interval: {Interval}s).", QrRefreshIntervalSeconds);
    await context.SetState(QrSessionState.AwaitingPeer, "Waiting for peer device to connect...");

    async Task<string> switchCallback()
    {
      if (context.CurrentStateEnum == QrSessionState.AwaitingPeer)
      {
        return await context.HubConnection.SwitchChannelAsync(_cancellationToken);
      }
      return null;
    }

    await context.QrCodeRefresher.StartAsync(switchCallback, TimeSpan.FromSeconds(QrRefreshIntervalSeconds), _cancellationToken);
  }

  public override async Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload)
  {
    if (messageType == "ChannelReady")
    {
      Logger.LogInformation("AwaitingPeerState: Received 'ChannelReady' message. Peer has joined.");

      if (context.CurrentRole == QrLoginRole.Requester)
      {
        Logger.LogInformation("Role=Requester: Sending DeviceInfoRequest and transitioning to AwaitingAuthorization.");
        await context.NotifySendDeviceInfo();
        await context.SetState(QrSessionState.AwaitingAuthorization, "Awaiting authorization...");
        await context.TransitionToAsync(new AwaitingAuthorizationState(Logger));
      }
      else
      {
        Logger.LogInformation("Role=Provider: Transitioning to AwaitingAuthorization.");
        await context.SetState(QrSessionState.PeerJoined, "Peer joined. Awaiting device info...");
        await context.TransitionToAsync(new AwaitingAuthorizationState(Logger));
      }
    }
    else
    {
      await base.HandleMessageAsync(context, messageType, payload);
    }
  }

  public override async Task HandleQrRefreshedAsync(QrLoginSessionManager context, string newChannelId)
  {
    Logger.LogInformation("AwaitingPeerState: QR Refresher provided new channel ID: {ChannelId}", newChannelId);
    context.ChannelId = newChannelId;
    context.NotifyQrCodeAvailable(newChannelId);
  }
}
