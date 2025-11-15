using System;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class ExchangingKeysState : QrLoginStateBase
{
  public ExchangingKeysState(ILogger logger) : base(logger) { }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    await context.SetState(QrSessionState.ExchangingKeys, "Anahtar değişimi yapılıyor...");

    if (context.IsPublicKeySent)
    {
      Logger.LogWarning("Already in ExchangingKeysState, public key already sent.");
      return;
    }

    try
    {
      var publicKeyBase64 = context.SecureChannelManager.InitiateKeyExchange();
      await context.HubConnection.SendMessageAsync("PublicKey", publicKeyBase64);
      context.IsPublicKeySent = true;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to initiate key exchange and send public key.");
      await context.HandleErrorAsync($"Anahtar değişimi başlatılamadı: {ex.Message}");
    }
  }

  public override async Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload)
  {
    if (messageType == "PublicKey")
    {
      try
      {
        Logger.LogInformation("Received counter-party public key. Finalizing key exchange.");

        context.SecureChannelManager.FinalizeKeyExchange(payload);

        await context.TransitionToAsync(new SecureChannelState(Logger));
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Failed to finalize key exchange with received public key.");
        await context.HandleErrorAsync($"Güvenli anahtar oluşturulamadı: {ex.Message}");
      }
    }
    else
    {
      await base.HandleMessageAsync(context, messageType, payload);
    }
  }
}
