using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.UI;

namespace SecureVault.App.Infrastructure.Services.Interaction.Handlers;

public class UserSessionRevokedHandler : ISignalRHubEventHandler
{
  private readonly ILogger<UserSessionRevokedHandler> _logger;
  private readonly IAuthenticationStateNotifier _authenticationStateNotifier;

  public UserSessionRevokedHandler(ILogger<UserSessionRevokedHandler> logger, IAuthenticationStateNotifier authenticationStateNotifier)
  {
    _logger = logger;
    _authenticationStateNotifier = authenticationStateNotifier;
  }

  public void RegisterHandlers(HubConnection connection)
  {
    connection.On("UserSessionRevoked", async () =>
    {
      _logger.LogInformation("Sunucudan 'UserSessionRevoked' bildirimi alındı. Kullanıcı oturumu iptal ediliyor.");
      await _authenticationStateNotifier.NotifyUserLogout();
    });
  }
}
