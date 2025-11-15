using System;
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
        await context.TransitionToAsync(new ExchangingKeysState(Logger));
      }
      else if (messageType == "AuthorizationDenied")
      {
        await context.HandleErrorAsync("Cihaz bağlantı isteği reddedildi.");
      }
    }
  }

  public override async Task ApproveAuthorizationAsync(QrLoginSessionManager context)
  {
    if (context.CurrentRole == QrLoginRole.Provider)
    {
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
      await context.HubConnection.SendMessageAsync("AuthorizationDenied", string.Empty);
      await context.SetState(QrSessionState.Idle, "Bağlantı isteği reddedildi.");
      await context.TransitionToAsync(new IdleState(Logger));
    }
    else
    {
      await base.DenyAuthorizationAsync(context);
    }
  }
}