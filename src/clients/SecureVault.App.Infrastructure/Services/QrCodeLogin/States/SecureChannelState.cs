
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class SecureChannelState : QrLoginStateBase
{
  public SecureChannelState(ILogger logger) : base(logger) { }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    Logger.LogInformation("Entering SecureChannelState: Secure channel is established. Awaiting credential transfer.");

    await context.SetState(QrSessionState.SecureChannelEstablished, "Secure channel established.");
  }

  public override async Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload)
  {
    if (context.CurrentRole == QrLoginRole.Requester)
    {
      if (messageType == "EncryptedLoginData")
      {
        Logger.LogInformation("SecureChannelState (Requester): Received 'EncryptedLoginData'. Attempting decryption...");
        await context.SetState(QrSessionState.TransferringCredentials, "Login credentials received, decrypting...");
        try
        {
          var encryptedBytes = Convert.FromBase64String(payload);
          var decryptedDto = context.SecureChannelManager.Decrypt<LoginQrCodeDto>(encryptedBytes);

          Logger.LogInformation("SecureChannelState (Requester): Decryption successful. Notifying orchestrator.");
          context.NotifyLoginCredentialsReceived(decryptedDto);

          await context.TransitionToAsync(new CompletedState(Logger));
        }
        catch (Exception ex)
        {
          Logger.LogError(ex, "Failed to decrypt login credentials.");
          await context.HandleErrorAsync($"Failed to decrypt login credentials: {ex.Message}");
        }
      }
      else
      {
        await base.HandleMessageAsync(context, messageType, payload);
      }
    }
    else
    {
      Logger.LogWarning("Provider role received unexpected message in SecureChannelState: {MessageType}", messageType);
    }
  }

  public override async Task SendCredentialsAsync(QrLoginSessionManager context, LoginQrCodeDto credentials)
  {
    if (context.CurrentRole != QrLoginRole.Provider)
    {
      await base.SendCredentialsAsync(context, credentials);
      return;
    }

    if (!context.SecureChannelManager.IsSecureChannelEstablished)
    {
      await context.HandleErrorAsync("Secure channel is not ready. Cannot send credentials.");
      return;
    }

    try
    {
      Logger.LogInformation("SecureChannelState (Provider): 'SendCredentialsAsync' called. Encrypting data...");
      await context.SetState(QrSessionState.TransferringCredentials, "Encrypting and sending login credentials...");

      var encryptedBytes = context.SecureChannelManager.Encrypt(credentials);
      var encryptedBase64 = Convert.ToBase64String(encryptedBytes);

      await context.HubConnection.SendMessageAsync("EncryptedLoginData", encryptedBase64);

      Logger.LogInformation("SecureChannelState (Provider): Successfully encrypted and sent 'EncryptedLoginData' message.");

      context.NotifyAuthorizationComplete();
      await context.TransitionToAsync(new CompletedState(Logger));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Failed to send credentials.");
      await context.HandleErrorAsync($"Failed to send credentials: {ex.Message}");
    }
  }
}
