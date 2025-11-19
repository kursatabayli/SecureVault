using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Session;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class AwaitingAuthorizationState : QrLoginStateBase
{
  public AwaitingAuthorizationState(ILogger logger) : base(logger) { }

  public override async Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload)
  {
    if (context.CurrentRole == QrLoginRole.Provider)
    {
      if (messageType == "DeviceInfoRequest")
      {
        Logger.LogInformation("AwaitingAuthorizationState (Provider): Received 'DeviceInfoRequest'. Deserializing and notifying UI...");
        var deviceInfo = JsonSerializer.Deserialize<DeviceDetailDto>(payload);
        if (deviceInfo != null)
        {
          context.NotifyDeviceAuthorizationRequired(deviceInfo);
        }
      }
    }
    else
    {
      if (messageType == "AuthorizationApproved")
      {
        Logger.LogInformation("AwaitingAuthorizationState (Requester): Received 'AuthorizationApproved'. Transitioning to key exchange.");
        await context.TransitionToAsync(new ExchangingKeysState(Logger));
      }
      else if (messageType == "AuthorizationDenied")
      {
        Logger.LogWarning("AwaitingAuthorizationState (Requester): Received 'AuthorizationDenied'.");
        await context.HandleErrorAsync("Device connection request was denied.");
      }
    }
  }

  public override async Task ApproveAuthorizationAsync(QrLoginSessionManager context)
  {
    if (context.CurrentRole == QrLoginRole.Provider)
    {
      Logger.LogInformation("AwaitingAuthorizationState (Provider): Authorization approved by user. Sending 'AuthorizationApproved' message...");
      await context.HubConnection.SendMessageAsync("AuthorizationApproved", string.Empty);
      await context.TransitionToAsync(new ExchangingKeysState(Logger));
    }
    else
    {
      await base.ApproveAuthorizationAsync(context);
    }
  }

  public override async Task DenyAuthorizationAsync(QrLoginSessionManager context)
  {
    if (context.CurrentRole == QrLoginRole.Provider)
    {
      Logger.LogInformation("AwaitingAuthorizationState (Provider): Authorization denied by user. Sending 'AuthorizationDenied' message...");
      await context.HubConnection.SendMessageAsync("AuthorizationDenied", string.Empty);
      await context.SetState(QrSessionState.Idle, "Connection request denied.");
      await context.TransitionToAsync(new IdleState(Logger));
    }
    else
    {
      await base.DenyAuthorizationAsync(context);
    }
  }
}