using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin;

public class QrLoginOrchestratorService : IQrLoginOrchestrator
{
    private readonly IQrLoginSessionManager _sessionManager;
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<QrLoginOrchestratorService> _logger;

    public event Func<QrSessionState, string, Task> OnStateChanged;
    public event Func<string, Task> OnQrCodeAvailable;
    public event Func<LoginQrCodeDto, Task> OnLoginCredentialsReceived;
    public event Func<Task> OnAuthorizationComplete;
    public event Func<DeviceDetailDto, Task> OnDeviceAuthorizationRequired;

    public QrLoginOrchestratorService(
        IQrLoginSessionManager sessionManager,
        IDispatcher dispatcher,
        ILogger<QrLoginOrchestratorService> logger)
    {
        _sessionManager = sessionManager;
        _dispatcher = dispatcher;
        _logger = logger;

        _sessionManager.StateChanged += HandleStateChanged;
        _sessionManager.QrCodeAvailable += HandleQrCodeAvailable;
        _sessionManager.LoginCredentialsReceived += HandleLoginCredentialsReceived;
        _sessionManager.DeviceAuthorizationRequired += HandleDeviceAuthorizationRequired;
        _sessionManager.AuthorizationComplete += HandleAuthorizationComplete;
    }

    #region UI Command Pass-through to Core Logic

    public Task StartSession(QrLoginRole role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UI Command: StartSession. Role: {Role}", role);
        return _sessionManager.StartSession(role, cancellationToken);
    }

    public Task JoinSessionByScanning(QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UI Command: JoinSessionByScanning. Role: {Role}, ChannelId: {ChannelId}", role, scannedChannelId);
        return _sessionManager.JoinSessionByScanning(role, scannedChannelId, cancellationToken);
    }

    public Task SendCredentials(bool rememberMeDecision)
    {
        _logger.LogInformation("UI Command: SendCredentials. RememberMe: {RememberMe}", rememberMeDecision);
        return _sessionManager.SendCredentials(rememberMeDecision);
    }

    public Task ApproveAuthorization()
    {
        _logger.LogInformation("UI Command: ApproveAuthorization.");
        return _sessionManager.ApproveAuthorization();
    }

    public Task DenyAuthorization()
    {
        _logger.LogInformation("UI Command: DenyAuthorization.");
        return _sessionManager.DenyAuthorization();
    }

    #endregion

    #region Event Bridging (Core Logic to UI)

    private async void HandleStateChanged(QrSessionState state, string message)
    {
        _logger.LogInformation("Core Event: StateChanged. New State: {State}, Message: {Message}", state, message);
        if (OnStateChanged == null) return;

        foreach (var handler in OnStateChanged.GetInvocationList().Cast<Func<QrSessionState, string, Task>>())
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    await handler(state, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnStateChanged subscriber.");
                }
            });
        }
    }
    private async void HandleQrCodeAvailable(string channelId)
    {
        _logger.LogInformation("Core Event: QrCodeAvailable. ChannelId: {ChannelId}", channelId);
        if (OnQrCodeAvailable == null) return;

        foreach (var handler in OnQrCodeAvailable.GetInvocationList().Cast<Func<string, Task>>())
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    await handler(channelId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnQrCodeAvailable subscriber.");
                }
            });
        }
    }
    private async void HandleLoginCredentialsReceived(LoginQrCodeDto credentials)
    {
        _logger.LogInformation("Core Event: LoginCredentialsReceived.");
        if (OnLoginCredentialsReceived == null) return;

        foreach (var handler in OnLoginCredentialsReceived.GetInvocationList().Cast<Func<LoginQrCodeDto, Task>>())
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    await handler(credentials);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnLoginCredentialsReceived subscriber.");
                }
            });
        }
    }
    private async void HandleDeviceAuthorizationRequired(DeviceDetailDto deviceInfo)
    {
        _logger.LogInformation("Core Event: DeviceAuthorizationRequired. Device: {DeviceName}", deviceInfo.DeviceName);
        if (OnDeviceAuthorizationRequired == null) return;

        foreach (var handler in OnDeviceAuthorizationRequired.GetInvocationList().Cast<Func<DeviceDetailDto, Task>>())
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    await handler(deviceInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnDeviceAuthorizationRequired subscriber.");
                }
            });
        }
    }
    private async void HandleAuthorizationComplete()
    {
        _logger.LogInformation("Core Event: AuthorizationComplete.");
        if (OnAuthorizationComplete == null) return;

        foreach (var handler in OnAuthorizationComplete.GetInvocationList().Cast<Func<Task>>())
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnAuthorizationComplete subscriber.");
                }
            });
        }
    }

    #endregion

    #region Lifecycle Management

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing Orchestrator (Adapter) and unsubscribing from Session Manager...");

        _sessionManager.StateChanged -= HandleStateChanged;
        _sessionManager.QrCodeAvailable -= HandleQrCodeAvailable;
        _sessionManager.LoginCredentialsReceived -= HandleLoginCredentialsReceived;
        _sessionManager.DeviceAuthorizationRequired -= HandleDeviceAuthorizationRequired;
        _sessionManager.AuthorizationComplete -= HandleAuthorizationComplete;

        await _sessionManager.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    #endregion
}