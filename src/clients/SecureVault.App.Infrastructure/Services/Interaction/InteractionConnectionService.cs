using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Application.Contracts.Abstractions.DPoP;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Infrastructure.Helpers;
using SecureVault.App.Infrastructure.HttpHandlers;
using System.Net.WebSockets;

namespace SecureVault.App.Infrastructure.Services.Interaction;

public class InteractionConnectionService : IInteractionConnectionService
{
    private readonly ILogger<InteractionConnectionService> _logger;
    private readonly IProtectedHttpHandlerPipelineBuilder _pipelineBuilder;
    private readonly IStorageService _storageService;
    private readonly IDpopProofService _dpopProofService;
    private readonly IEnumerable<ISignalRHubEventHandler> _hubEventHandlers;
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public InteractionConnectionService(ILogger<InteractionConnectionService> logger,
     IProtectedHttpHandlerPipelineBuilder pipelineBuilder,
     IStorageService storageService,
     IDpopProofService dpopProofService,
     IEnumerable<ISignalRHubEventHandler> hubEventHandlers,
     IOptions<ApiSettings> apiSettings,
     IOptions<SignalRSettings> signalRSettings)
    {
        _logger = logger;
        _pipelineBuilder = pipelineBuilder;
        _storageService = storageService;
        _dpopProofService = dpopProofService;
        _hubEventHandlers = hubEventHandlers;
        var baseUrl = new Uri(apiSettings.Value.BaseUrl);
        var fullHubUri = new Uri(baseUrl, signalRSettings.Value.HubPath);
        _hubUrl = fullHubUri.ToString();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            _logger.LogInformation("ConnectAsync called. Current SignalR state: {State}", _connection.State);
            if (_connection.State is HubConnectionState.Connected or HubConnectionState.Connecting)
            {
                return;
            }

            await _connection.DisposeAsync();
            _logger.LogInformation("Disposed previous disconnected HubConnection.");
        }

        async ValueTask<WebSocket> CreateWebSocketFactoryAsync(WebSocketConnectionContext context, CancellationToken factoryCancellationToken)
        {
            var dws = new ClientWebSocket();
            _logger.LogDebug("WebSocketFactory: Creating new WebSocket connection...");
            try
            {
                var currentAccessToken = await _storageService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(currentAccessToken))
                {
                    _logger.LogError("WebSocketFactory: Access Token not found.");
                    throw new InvalidOperationException("WebSocketFactory: Access Token not found.");
                }

                var uriBuilder = new UriBuilder(context.Uri.GetLeftPart(UriPartial.Path));
                if (uriBuilder.Scheme == "wss")
                    uriBuilder.Scheme = "https";

                var htu = uriBuilder.Uri.AbsoluteUri;

                _logger.LogDebug("WebSocketFactory: Calculated HTU for DPoP proof: {Htu}", htu);

                var dpopProof = await _dpopProofService.CreateProofAsync("GET", htu, currentAccessToken);

                dws.Options.SetRequestHeader("DPoP", dpopProof);
                dws.Options.SetRequestHeader("Authorization", $"DPoP {currentAccessToken}");

                _logger.LogDebug("WebSocketFactory: Connecting to {Uri} with DPoP headers...", context.Uri);
                await dws.ConnectAsync(context.Uri, factoryCancellationToken);
                return dws;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocketFactory while creating DPoP proof or connecting.");
                dws.Dispose();
                throw;
            }
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.HttpMessageHandlerFactory = _ => _pipelineBuilder.CreatePipeline();
                options.WebSocketFactory = CreateWebSocketFactoryAsync;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += OnConnectionClosed;
        _connection.Reconnecting += OnConnectionReconnecting;
        _connection.Reconnected += OnConnectionReconnected;

        RegisterAllHandlers();

        try
        {
            _logger.LogInformation("Connecting to SignalR Hub...");
            await _connection.StartAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to SignalR Hub. ConnectionId: {ConnectionId}", _connection.ConnectionId);
            await RegisterDeviceAfterConnection(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while connecting to SignalR Hub.");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            _logger.LogInformation("Disconnecting from SignalR Hub...");
            await _connection.StopAsync();
            Dispose();
            await _connection.DisposeAsync();
            _logger.LogInformation("Successfully disconnected from SignalR Hub.");
        }
    }

    public async Task NotifySyncRequiredAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is null || _connection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("SignalR connection is not active. Sync notification could not be sent.");
            return;
        }

        try
        {
            await _connection.InvokeAsync("NotifySyncRequired", cancellationToken);
            _logger.LogInformation("Successfully sent 'NotifySyncRequired' notification to server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending 'NotifySyncRequired' notification.");
        }
    }

    public async Task NotifyUserSessionRevokedAsync(string userDeviceId, CancellationToken cancellationToken = default)
    {
        if (_connection is null || _connection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("SignalR connection is not active. Session revocation notification could not be sent.");
            return;
        }

        try
        {
            await _connection.InvokeAsync("NotifyUserSessionRevoked", userDeviceId, cancellationToken);
            _logger.LogInformation("Successfully sent 'NotifyUserSessionRevoked' notification to server.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending 'NotifyUserSessionRevoked' notification.");
        }
    }

    private void RegisterAllHandlers()
    {
        if (_connection == null) return;
        _logger.LogInformation("Registering SignalR event handlers...");
        foreach (var handler in _hubEventHandlers)
        {
            _logger.LogDebug("Registering handlers from: {HandlerName}", handler.GetType().Name);
            handler.RegisterHandlers(_connection);
        }
        _logger.LogInformation("All {Count} SignalR event handlers registered.", _hubEventHandlers.Count());
    }

    private async Task RegisterDeviceAfterConnection(CancellationToken cancellationToken)
    {
        try
        {
            var uniqueDeviceId = await _storageService.GetUniqueDeviceIdAsync();
            if (!string.IsNullOrEmpty(uniqueDeviceId))
            {
                if (_connection is not null)
                {
                    await _connection.InvokeAsync("RegisterActiveDevice", uniqueDeviceId, cancellationToken);
                    _logger.LogInformation("Active device registered with server as {DeviceId}.", uniqueDeviceId);
                }
            }
            else
            {
                _logger.LogWarning("Could not get UniqueDeviceId, device not registered.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking 'RegisterActiveDevice'.");
        }
    }

    private Task OnConnectionClosed(Exception? error)
    {
        _logger.LogError(error, "SignalR connection closed. Error: {Error}", error?.Message);
        return Task.CompletedTask;
    }

    private Task OnConnectionReconnecting(Exception? error)
    {
        _logger.LogWarning(error, "SignalR connection lost. Attempting to reconnect... Error: {Error}", error?.Message);
        return Task.CompletedTask;
    }

    private Task OnConnectionReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR connection re-established. New ConnectionId: {ConnectionId}", connectionId);

        _logger.LogInformation("Re-registering device on server after reconnect...");
        _ = RegisterDeviceAfterConnection(CancellationToken.None);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_connection == null) return;

        _logger.LogDebug("Unsubscribing from SignalR connection events (Closed, Reconnecting, Reconnected)...");
        _connection.Closed -= OnConnectionClosed;
        _connection.Reconnecting -= OnConnectionReconnecting;
        _connection.Reconnected -= OnConnectionReconnected;
    }
}
