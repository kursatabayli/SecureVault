using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Abstractions.UI;

namespace SecureVault.App.Services
{
    public class AppLifecycleManager : IAppLifecycleManager
    {
        private readonly IBackgroundSyncService _backgroundSyncService;
        private readonly IInteractionConnectionService _interactionConnectionService;
        private readonly ILogger<AppLifecycleManager> _logger;
        private readonly IStorageService _storageService;
        private readonly IAuthenticationStateNotifier _authenticationStateNotifier;

        private CancellationTokenSource _appCts = new();
        private bool _servicesAreActive = false;

        private readonly SemaphoreSlim _serviceManagementLock = new(1, 1);
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        public AppLifecycleManager(
            IBackgroundSyncService backgroundSyncService,
            IInteractionConnectionService interactionConnectionService,
            ILogger<AppLifecycleManager> logger,
            IStorageService storageService,
            IAuthenticationStateNotifier authenticationStateNotifier)
        {
            _backgroundSyncService = backgroundSyncService;
            _interactionConnectionService = interactionConnectionService;
            _logger = logger;
            _storageService = storageService;
            _authenticationStateNotifier = authenticationStateNotifier;
        }

        public void Initialize()
        {
            _authenticationStateNotifier.OnAuthenticationStateChangedAsync += HandleAuthenticationStateChange;
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
        }

        public Task OnStart() => UpdateServicesStateAsync();
        public Task OnResume() => UpdateServicesStateAsync();
        public Task OnSleep() => StopPersistentServicesAsync();
        public Task OnDestroying() => StopPersistentServicesAsync();

        private async Task UpdateServicesStateAsync()
        {
            await _serviceManagementLock.WaitAsync();
            try
            {
                bool isAuthenticated = _storageService.IsAuthenticated();
                bool hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
                bool shouldBeRunning = isAuthenticated && hasInternet;

                _logger.LogInformation(
                    "Servis durumu kontrol ediliyor. Kimlik: {IsAuthenticated}, İnternet: {HasInternet}, Çalışmalı mı?: {ShouldBeRunning}, Mevcut Durum: {IsActive}",
                    isAuthenticated, hasInternet, shouldBeRunning, _servicesAreActive);

                if (shouldBeRunning)
                {
                    if (!_servicesAreActive)
                        await StartPersistentServicesAsync();

                    await RunOneTimeSyncAsync();
                }
                else if (!shouldBeRunning && _servicesAreActive)
                {
                    await StopPersistentServicesAsync();
                }
            }
            finally
            {
                _serviceManagementLock.Release();
            }
        }

        private async Task HandleAuthenticationStateChange(bool isAuthenticated)
        {
            _logger.LogInformation($"Authentication durumu değişti (isAuthenticated: {isAuthenticated}), servisler yeniden değerlendiriliyor.");
            await UpdateServicesStateAsync();
        }

        private async void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            try
            {
                _logger.LogInformation($"İnternet durumu değişti: {e.NetworkAccess}");
                await UpdateServicesStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnConnectivityChanged sırasında bir hata oluştu.");
            }
        }

        private async Task RunOneTimeSyncAsync()
        {
            if (!await _syncLock.WaitAsync(0))
            {
                _logger.LogInformation("Zaten devam eden bir senkronizasyon var, yenisi atlanıyor.");
                return;
            }
            try
            {
                if (_appCts != null)
                    await _backgroundSyncService.SynchronizeAsync(_appCts.Token);
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task StartPersistentServicesAsync()
        {
            if (_servicesAreActive) return;
            _logger.LogInformation("Kalıcı arka plan servisleri (SignalR) başlatılıyor.");
            _servicesAreActive = true;

            if (_appCts == null || _appCts.IsCancellationRequested)
            {
                _appCts?.Dispose();
                _appCts = new CancellationTokenSource();
            }

            await _interactionConnectionService.ConnectAsync(_appCts.Token);
        }

        private async Task StopPersistentServicesAsync()
        {
            if (!_servicesAreActive) return;
            _logger.LogInformation("Kalıcı arka plan servisleri (SignalR) durduruluyor.");
            _servicesAreActive = false;

            if (_appCts != null && !_appCts.IsCancellationRequested)
                _appCts.Cancel();

            await _interactionConnectionService.DisconnectAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await StopPersistentServicesAsync();

            _authenticationStateNotifier.OnAuthenticationStateChangedAsync -= HandleAuthenticationStateChange;
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
            _appCts?.Dispose();
            _serviceManagementLock?.Dispose();
            _syncLock?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
