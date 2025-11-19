using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Services.Interfaces;

namespace SecureVault.App.Services.Implementations;

public class AppLifecycleManager : IAppLifecycleManager
{
    private readonly IBackgroundSyncService _backgroundSyncService;
    private readonly IInteractionConnectionService _interactionConnectionService;
    private readonly IStorageService _storageService;
    private readonly IAuthenticationStateNotifier _authenticationStateNotifier;
    private readonly ILogger<AppLifecycleManager> _logger;

    private CancellationTokenSource _appCts = new();
    private bool _servicesAreActive = false;

    private readonly SemaphoreSlim _serviceManagementLock = new(1, 1);
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public AppLifecycleManager(
        IBackgroundSyncService backgroundSyncService,
        IInteractionConnectionService interactionConnectionService,
        IStorageService storageService,
        IAuthenticationStateNotifier authenticationStateNotifier,
        ILogger<AppLifecycleManager> logger)
    {
        _backgroundSyncService = backgroundSyncService;
        _interactionConnectionService = interactionConnectionService;
        _storageService = storageService;
        _authenticationStateNotifier = authenticationStateNotifier;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInformation("Initializing AppLifecycleManager event subscriptions...");
        _authenticationStateNotifier.OnAuthenticationStateChangedAsync += HandleAuthenticationStateChange;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    public Task OnStart() => UpdateServicesStateAsync("OnStart");
    public Task OnResume() => UpdateServicesStateAsync("OnResume");
    public Task OnSleep() => StopPersistentServicesAsync("OnSleep");
    public Task OnDestroying() => StopPersistentServicesAsync("OnDestroying");

    private async Task UpdateServicesStateAsync(string trigger)
    {
        _logger.LogDebug("Waiting for service management lock. Trigger: {Trigger}", trigger);
        await _serviceManagementLock.WaitAsync();
        try
        {
            bool isAuthenticated = _storageService.IsAuthenticated();
            bool hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            bool shouldBeRunning = isAuthenticated && hasInternet;

            _logger.LogInformation(
                "Checking service state (Trigger: {Trigger}). IsAuthenticated: {IsAuthenticated}, HasInternet: {HasInternet}, ShouldBeRunning: {ShouldBeRunning}, IsActive: {IsActive}",
                trigger, isAuthenticated, hasInternet, shouldBeRunning, _servicesAreActive);

            if (shouldBeRunning)
            {
                if (!_servicesAreActive)
                {
                    await StartPersistentServicesAsync(trigger);
                }

                await RunOneTimeSyncAsync(trigger);
            }
            else if (!shouldBeRunning && _servicesAreActive)
            {
                await StopPersistentServicesAsync(trigger);
            }
        }
        finally
        {
            _serviceManagementLock.Release();
        }
    }

    private async Task HandleAuthenticationStateChange(bool isAuthenticated)
    {
        _logger.LogInformation("Authentication state changed (IsAuthenticated: {IsAuthenticated}), re-evaluating services.", isAuthenticated);
        await UpdateServicesStateAsync("HandleAuthenticationStateChange");
    }

    private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Connectivity state changed. NetworkAccess: {NetworkAccess}, Profiles: {Profiles}", e.NetworkAccess, string.Join(",", e.ConnectionProfiles));
            await UpdateServicesStateAsync("OnConnectivityChanged");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred in OnConnectivityChanged.");
        }
    }

    private async Task RunOneTimeSyncAsync(string trigger)
    {
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogInformation("A synchronization is already in progress, skipping new request. Trigger: {Trigger}", trigger);
            return;
        }

        _logger.LogInformation("Starting one-time synchronization... Trigger: {Trigger}", trigger);
        try
        {
            if (_appCts != null && !_appCts.IsCancellationRequested)
            {
                await _backgroundSyncService.SynchronizeAsync(_appCts.Token);
                _logger.LogInformation("One-time synchronization finished.");
            }
            else
            {
                _logger.LogWarning("Skipped one-time synchronization as the application token source is null or canceled.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("One-time synchronization was canceled. Trigger: {Trigger}", trigger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "One-time synchronization failed. Trigger: {Trigger}", trigger);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task StartPersistentServicesAsync(string trigger)
    {
        if (_servicesAreActive) return;
        _logger.LogInformation("Starting persistent background services (e.g., SignalR)... Trigger: {Trigger}", trigger);

        if (_appCts == null || _appCts.IsCancellationRequested)
        {
            _logger.LogDebug("Re-creating CancellationTokenSource as it was null or requested cancellation.");
            _appCts?.Dispose();
            _appCts = new CancellationTokenSource();
        }

        try
        {
            await _interactionConnectionService.ConnectAsync(_appCts.Token);
            _servicesAreActive = true;
            _logger.LogInformation("Persistent services started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start persistent services. Trigger: {Trigger}", trigger);
        }
    }

    private async Task StopPersistentServicesAsync(string trigger)
    {
        if (!_servicesAreActive) return;
        _logger.LogInformation("Stopping persistent background services (e.g., SignalR)... Trigger: {Trigger}", trigger);

        _servicesAreActive = false;

        if (_appCts != null && !_appCts.IsCancellationRequested)
        {
            _logger.LogDebug("Cancelling application CancellationTokenSource...");
            _appCts.Cancel();
        }

        try
        {
            await _interactionConnectionService.DisconnectAsync();
            _logger.LogInformation("Persistent services stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while disconnecting persistent services. Trigger: {Trigger}", trigger);
        }
    }


    public async ValueTask DisposeAsync()
    {
        _authenticationStateNotifier.OnAuthenticationStateChangedAsync -= HandleAuthenticationStateChange;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;

        await StopPersistentServicesAsync("DisposeAsync");

        _appCts?.Dispose();
        _serviceManagementLock?.Dispose();
        _syncLock?.Dispose();
    }
}
