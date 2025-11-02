using Microsoft.AspNetCore.SignalR.Client;

namespace SecureVault.App.Application.Contracts.Abstractions.Interaction;

public interface ISignalRHubEventHandler
{
  void RegisterHandlers(HubConnection connection);
}
