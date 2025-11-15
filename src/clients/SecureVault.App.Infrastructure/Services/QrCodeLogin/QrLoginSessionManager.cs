using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Device;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.App.Infrastructure.Services.QrCodeLogin.States;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin;

public class QrLoginSessionManager : IQrLoginSessionManager
{
  public IQrHubConnection HubConnection { get; }
  public ISecureChannelManager SecureChannelManager { get; }
  public IDeviceInfoService DeviceInfoService { get; }
  public IQrCodeRefresher QrCodeRefresher { get; }
  public IStorageService StorageService { get; }
  public ILogger Logger { get; }

  public string ChannelId { get; set; }
  public QrLoginRole CurrentRole { get; set; }
  public QrSessionState CurrentStateEnum { get; private set; } = QrSessionState.Idle;
  public bool IsPublicKeySent { get; set; } = false;

  public event Action<QrSessionState, string> StateChanged;
  public event Action<string> QrCodeAvailable;
  public event Action<LoginQrCodeDto> LoginCredentialsReceived;
  public event Action<DeviceDetailDto> DeviceAuthorizationRequired;
  public event Action AuthorizationComplete;

  private IQrLoginState _currentState;

  private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

  public QrLoginSessionManager(
      IQrHubConnection hubConnection,
      ISecureChannelManager secureChannelManager,
      IDeviceInfoService deviceInfoService,
      IStorageService storageService,
      ILogger<QrLoginSessionManager> logger,
      IQrCodeRefresher qrCodeRefresher)
  {
    HubConnection = hubConnection;
    SecureChannelManager = secureChannelManager;
    DeviceInfoService = deviceInfoService;
    StorageService = storageService;
    Logger = logger;
    QrCodeRefresher = qrCodeRefresher;

    HubConnection.OnMessageReceived += HandleReceivedMessage;
    HubConnection.OnErrorReceived += HandleErrorReceived;
    QrCodeRefresher.OnQrCodeAvailable += HandleQrRefreshed;
    QrCodeRefresher.OnError += HandleQrRefreshError;

    _currentState = new IdleState(logger);
    _currentState.HandleEnterAsync(this).ConfigureAwait(false);
  }

  #region State Geçişi ve Event Tetikleme (State'lerin kullanması için)

  public async Task TransitionToAsync(IQrLoginState newState)
  {
    Logger.LogInformation("Transitioning from {OldState} to {NewState}",
        _currentState.GetType().Name, newState.GetType().Name);

    _currentState = newState;

    await _currentState.HandleEnterAsync(this);
  }

  public Task SetState(QrSessionState newState, string message, bool force = false)
  {
    if (CurrentStateEnum == newState && !force) return Task.CompletedTask;

    var oldState = CurrentStateEnum;
    CurrentStateEnum = newState;

    if (oldState == QrSessionState.AwaitingPeer && newState != QrSessionState.AwaitingPeer)
      QrCodeRefresher.StopAsync().ConfigureAwait(false);

    Logger.LogInformation("State changed to {State}: {Message}", newState, message);
    StateChanged?.Invoke(newState, message);
    return Task.CompletedTask;
  }

  public Task HandleErrorAsync(string errorMessage)
  {
    return _currentState.HandleErrorAsync(this, errorMessage);
  }

  public void NotifyQrCodeAvailable(string channelId) => QrCodeAvailable?.Invoke(channelId);
  public void NotifyLoginCredentialsReceived(LoginQrCodeDto dto) => LoginCredentialsReceived?.Invoke(dto);
  public void NotifyDeviceAuthorizationRequired(DeviceDetailDto dto) => DeviceAuthorizationRequired?.Invoke(dto);
  public void NotifyAuthorizationComplete() => AuthorizationComplete?.Invoke();
  public async Task NotifySendDeviceInfo()
  {
    var deviceInfo = new DeviceDetailDto
    {
      UniqueDeviceId = await DeviceInfoService.GetUniqueDeviceIdAsync(),
      DeviceModel = DeviceInfoService.GetDeviceModel(),
      DeviceName = DeviceInfoService.GetDeviceName(),
      DeviceManufacturer = DeviceInfoService.GetDeviceManufacturer(),
      OperatingSystem = DeviceInfoService.GetOperatingSystemInfo()
    };
    var payload = JsonSerializer.Serialize(deviceInfo);
    await HubConnection.SendMessageAsync("DeviceInfoRequest", payload);
  }

  #endregion

  #region IQrLoginSessionManager Implementasyonu (UI Komutları -> State'e Delege Et)

  public async Task StartSession(QrLoginRole role, CancellationToken cancellationToken = default)
  {
    await _stateLock.WaitAsync(cancellationToken);
    try
    {
      await _currentState.StartSessionAsync(this, role, cancellationToken);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  public async Task JoinSessionByScanning(QrLoginRole role, string scannedChannelId, CancellationToken cancellationToken = default)
  {
    await _stateLock.WaitAsync(cancellationToken);
    try
    {
      await _currentState.JoinSessionByScanningAsync(this, role, scannedChannelId, cancellationToken);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  public async Task SendCredentials(bool rememberMeDecision)
  {
    await _stateLock.WaitAsync();
    try
    {
      var credentials = new LoginQrCodeDto
      {
        Email = StorageService.GetEmail(),
        PrivateKey = await StorageService.GetPrivateKeyAsByteAsync(),
        EncryptionKey = await StorageService.GetEncryptionKeyAsByteAsync(),
        RememberMe = rememberMeDecision
      };
      await _currentState.SendCredentialsAsync(this, credentials);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  public async Task ApproveAuthorization()
  {
    await _stateLock.WaitAsync();
    try
    {
      await _currentState.ApproveAuthorizationAsync(this);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  public async Task DenyAuthorization()
  {
    await _stateLock.WaitAsync();
    try
    {
      await _currentState.DenyAuthorizationAsync(this);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  #endregion

  #region Olay Dinleyicileri (Event Handlers -> State'e Delege Et)

  private async Task HandleReceivedMessage(string messageType, string payload)
  {
    await _stateLock.WaitAsync();
    try
    {
      await _currentState.HandleMessageAsync(this, messageType, payload);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  private async Task HandleErrorReceived(string errorMessage)
  {
    await _stateLock.WaitAsync();
    try
    {
      await HandleErrorAsync($"Sunucu hatası: {errorMessage}");
    }
    finally
    {
      _stateLock.Release();
    }
  }

  private async Task HandleQrRefreshed(string newChannelId)
  {
    await _stateLock.WaitAsync();
    try
    {
      await _currentState.HandleQrRefreshedAsync(this, newChannelId);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  private async Task HandleQrRefreshError(string errorMessage)
  {
    await _stateLock.WaitAsync();
    try
    {
      await HandleErrorAsync(errorMessage);
    }
    finally
    {
      _stateLock.Release();
    }
  }

  #endregion

  #region Yaşam Döngüsü (Lifecycle)

  public async ValueTask DisposeAsync()
  {
    Logger.LogInformation("Disposing QrLoginSessionManager (Context)...");

    HubConnection.OnMessageReceived -= HandleReceivedMessage;
    HubConnection.OnErrorReceived -= HandleErrorReceived;
    QrCodeRefresher.OnQrCodeAvailable -= HandleQrRefreshed;
    QrCodeRefresher.OnError -= HandleQrRefreshError;

    await QrCodeRefresher.DisposeAsync();
    await HubConnection.DisposeAsync();
  }

  #endregion
}