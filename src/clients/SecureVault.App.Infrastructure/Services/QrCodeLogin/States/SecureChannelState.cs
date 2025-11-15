
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class SecureChannelState : QrLoginStateBase
{
  public SecureChannelState(ILogger logger) : base(logger) { }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    await context.SetState(QrSessionState.SecureChannelEstablished, "Güvenli kanal kuruldu.");
  }

  public override async Task HandleMessageAsync(QrLoginSessionManager context, string messageType, string payload)
  {
    if (context.CurrentRole == QrLoginRole.Requester)
    {
      if (messageType == "EncryptedLoginData")
      {
        await context.SetState(QrSessionState.TransferringCredentials, "Oturum bilgileri alındı, doğrulanıyor...");
        try
        {
          var encryptedBytes = Convert.FromBase64String(payload);
          var decryptedDto = context.SecureChannelManager.Decrypt<LoginQrCodeDto>(encryptedBytes);

          context.NotifyLoginCredentialsReceived(decryptedDto);

          await context.TransitionToAsync(new CompletedState(Logger));
        }
        catch (Exception ex)
        {
          Logger.LogError(ex, "Oturum bilgileri çözülemedi.");
          await context.HandleErrorAsync($"Oturum bilgileri çözülemedi: {ex.Message}");
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
      await context.HandleErrorAsync("Güvenli kanal hazır değil. Kimlik bilgileri gönderilemez.");
      return;
    }

    try
    {
      await context.SetState(QrSessionState.TransferringCredentials, "Oturum bilgileri şifrelenip gönderiliyor...");

      var encryptedBytes = context.SecureChannelManager.Encrypt(credentials);
      var encryptedBase64 = Convert.ToBase64String(encryptedBytes);

      await context.HubConnection.SendMessageAsync("EncryptedLoginData", encryptedBase64);

      context.NotifyAuthorizationComplete();

      await context.TransitionToAsync(new CompletedState(Logger));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Kimlik bilgileri gönderilemedi.");
      await context.HandleErrorAsync($"Kimlik bilgileri gönderilemedi: {ex.Message}");
    }
  }
}
