using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

public class CompletedState : QrLoginStateBase
{
  public CompletedState(ILogger logger) : base(logger) { }

  public override async Task HandleEnterAsync(QrLoginSessionManager context)
  {
    await context.SetState(QrSessionState.Completed, "Yetkilendirme tamamlandı.", true);

    await context.QrCodeRefresher.StopAsync();

    Logger.LogInformation("QR Login session completed successfully.");
  }
}