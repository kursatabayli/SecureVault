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
    await context.SetState(QrSessionState.CreatingChannel, "Oturuma katılım sağlanıyor...");

    try
    {
      await context.HubConnection.StartConnectionAsync(context.ChannelId, _cancellationToken);

      await context.TransitionToAsync(new AwaitingPeerState(Logger, _cancellationToken));
    }
    catch (OperationCanceledException)
    {
      Logger.LogInformation("Kullanıcı tarafından oturum oluşturma iptal edildi.");
      await context.TransitionToAsync(new IdleState(Logger));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex, "Oturuma katılım sağlanamadı.");
      await context.HandleErrorAsync($"Oturuma katılım sağlanamadı: {ex.Message}");
    }
  }
}