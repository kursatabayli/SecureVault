using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class ExchangingKeysState : QrLoginStateBase
{
  public ExchangingKeysState(ILogger logger) : base(logger) { }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    Logger.LogInformation("Entering ExchangingKeysState. Initiating ECDH key exchange...");
    await context.SetState(QrSessionState.ExchangingKeys, "Exchanging keys...");

    if (context.IsPublicKeySent)
    {
      Logger.LogWarning("Already in ExchangingKeysState, public key already sent.");
      return;
    }

    try
    {
      var publicKeyBase64 = context.SecureChannelManager.InitiateKeyExchange();

      Logger.LogInformation("Sending local public key to peer...");
      await context.HubConnection.SendMessageAsync("PublicKey", publicKeyBase64);
      context.IsPublicKeySent = true;
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to initiate key exchange and send public key.");
      await context.HandleErrorAsync($"Failed to initiate key exchange: {ex.Message}");
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

        Logger.LogInformation("Key exchange finalized. Transitioning to SecureChannelState.");
        await context.TransitionToAsync(new SecureChannelState(Logger));
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Failed to finalize key exchange with received public key.");
        await context.HandleErrorAsync($"Failed to create secure key: {ex.Message}");
      }
    }
    else
    {
      await base.HandleMessageAsync(context, messageType, payload);
    }
  }
}
