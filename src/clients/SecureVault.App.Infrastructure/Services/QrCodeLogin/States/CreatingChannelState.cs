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
    await context.SetState(QrSessionState.CreatingChannel, "Oturum kanalı oluşturuluyor...");

    try
    {
      var result = await context.HubConnection.StartConnectionAsync(null, cancellationToken: _cancellationToken);
      context.ChannelId = result;

      if (string.IsNullOrEmpty(context.ChannelId))
      {
        await context.HandleErrorAsync("Sunucudan kanal ID'si alınamadı.");
        return;
      }

      context.NotifyQrCodeAvailable(context.ChannelId);
      await context.TransitionToAsync(new AwaitingPeerState(Logger, _cancellationToken));
    }
    catch (OperationCanceledException)
    {
      Logger.LogInformation("Kullanıcı tarafından oturum oluşturma iptal edildi.");
      await context.TransitionToAsync(new IdleState(Logger));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Oturum başlatılamadı.");
      await context.HandleErrorAsync($"Oturum başlatılamadı: {ex.Message}");
    }
  }
}
