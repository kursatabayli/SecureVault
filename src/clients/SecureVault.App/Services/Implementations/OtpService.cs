using AutoMapper;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OtpNet;
using Realms;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.App.Domain.Enums;
using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.App.Resources.Localization;
using SecureVault.App.Services.Interfaces;
using SecureVault.Shared.Result;
using OtpType = SecureVault.App.Domain.Enums.OtpType;

namespace SecureVault.App.Services.Implementations;

public class OtpService : IOtpService, IAsyncDisposable
{
    private readonly ITwoFactorAuthCodeRepository _twoFactorAuthCodeRepository;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ILogger<OtpService> _logger;
    private readonly List<OtpViewModel> _displayItems = [];
    private readonly CancellationTokenSource _cts = new();
    private IRealmCollection<TwoFactorAuthCodeEntity> _liveCollection;
    private IDisposable _notificationToken;

    public bool IsLoading { get; private set; } = true;
    public Error? InitializationError { get; private set; }
    public event Func<Task>? OnTick;
    public IReadOnlyList<OtpViewModel> Items => _displayItems.AsReadOnly();

    private bool _isLoopStarted = false;
    private Task? _updateLoopTask;

    public OtpService(ITwoFactorAuthCodeRepository twoFactorAuthCodeRepository, IMapper mapper, IStringLocalizer<SharedResources> localizer, ILogger<OtpService> logger)
    {
        _twoFactorAuthCodeRepository = twoFactorAuthCodeRepository;
        _mapper = mapper;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("OtpService initialization started...");
        IsLoading = true;
        InitializationError = null;
        await InvokeOnTickAsync();

        try
        {
            _liveCollection = await _twoFactorAuthCodeRepository.GetLiveCollectionAsync();
            _notificationToken = _liveCollection.SubscribeForNotifications(OnDataChanged);
            LoadDataAndGenerateInitialCodes(_liveCollection);

            _logger.LogInformation("OtpService initialized. {ItemCount} OTP items loaded.", _displayItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while loading TOTP data.");
            InitializationError = new Error(ErrorCodes.Client.LoadFailed, _localizer[ErrorCodes.Client.LoadFailed]);
        }
        finally
        {
            IsLoading = false;
            await InvokeOnTickAsync();
        }

        if (!_isLoopStarted)
        {
            _updateLoopTask = StartUiUpdateLoop();
            _isLoopStarted = true;
        }
    }
    private async void OnDataChanged(IRealmCollection<TwoFactorAuthCodeEntity> sender, ChangeSet? changes)
    {
        if (changes != null)
        {
            _logger.LogInformation(
                "Realm data changed. Insertions: {Insertions}, Deletions: {Deletions}, Modifications: {Modifications}, Moves: {Moves}",
                changes.InsertedIndices.Length,
                changes.DeletedIndices.Length,
                changes.ModifiedIndices.Length,
                changes.Moves.Length);
        }
        else
        {
            _logger.LogInformation("Realm data changed (full collection reload).");
        }

        try
        {
            LoadDataAndGenerateInitialCodes(sender);
            await InvokeOnTickAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Realm data change notification.");
        }
    }
    public OtpViewModel? GetItem(Guid id)
    {
        return _displayItems.FirstOrDefault(i => i.Model.Id == id);
    }

    private void LoadDataAndGenerateInitialCodes(IRealmCollection<TwoFactorAuthCodeEntity> data)
    {
        var twoFactorAuthModel = _mapper.Map<List<TwoFactorAuthCodeModel>>(data);

        _displayItems.Clear();
        _displayItems.AddRange(twoFactorAuthModel.Select(model => new OtpViewModel { Model = model }));

        foreach (var item in _displayItems)
        {
            item.OtpGenerator = CreateOtpGenerator(item.Model);
            GenerateNewCode(item);
            UpdateTimeLeft(item);
        }
        _logger.LogDebug("Loaded and generated initial codes for {ItemCount} items.", _displayItems.Count);
    }

    private async Task StartUiUpdateLoop()
    {
        _logger.LogInformation("Starting UI update loop...");
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, _cts.Token);

                if (_displayItems.Count == 0) continue;

                foreach (var item in _displayItems)
                {
                    if (item.OtpGenerator is Totp totpGenerator)
                    {
                        int previousTimeLeft = item.TimeLeft;
                        OtpService.UpdateTimeLeft(item);

                        if (item.TimeLeft > previousTimeLeft)
                        {
                            GenerateNewCode(item);
                        }
                    }
                }

                await InvokeOnTickAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("UI update loop stopping due to cancellation request.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOTP UI update loop encountered an unhandled exception.");
            }
        }
        _logger.LogInformation("UI update loop stopped.");
    }
    private static void UpdateTimeLeft(OtpViewModel item)
    {
        if (item.OtpGenerator is Totp totpGenerator)
        {
            item.TimeLeft = totpGenerator.RemainingSeconds();
            item.ProgressValue = (double)item.TimeLeft / item.Model.Period * 100;
        }
        else
        {
            item.TimeLeft = 0;
            item.ProgressValue = 0;
        }
    }

    private void GenerateNewCode(OtpViewModel item)
    {
        if (item.OtpGenerator == null)
        {
            item.CurrentCode = _localizer[SharedResources.Text_Error_General];
            item.HasError = true;
            return;
        }

        item.HasError = false;

        if (item.OtpGenerator is Totp totpGenerator)
        {
            item.CurrentCode = totpGenerator.ComputeTotp();
        }
        else if (item.OtpGenerator is Hotp hotpGenerator)
        {
            if (item.Model.Counter > 0)
            {
                item.CurrentCode = hotpGenerator.ComputeHOTP(item.Model.Counter);
            }
            else
            {
                item.CurrentCode = _localizer[SharedResources.Text_ClickToGenerate];
            }
        }
    }
    private Otp? CreateOtpGenerator(TwoFactorAuthCodeModel model)
    {
        byte[] secretKeyBytes;
        try
        {
            secretKeyBytes = Base32Encoding.ToBytes(model.SecretKey);
            if (secretKeyBytes == null || secretKeyBytes.Length == 0)
            {
                _logger.LogWarning("Secret key for item {ItemId} is null or empty. Skipping this item.", model.Id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode Base32 secret key for item {ItemId}. Skipping this item.", model.Id);
            return null;
        }

        var hashMode = model.Algorithm switch
        {
            OtpAlgorithm.SHA256 => OtpHashMode.Sha256,
            OtpAlgorithm.SHA512 => OtpHashMode.Sha512,
            _ => OtpHashMode.Sha1,
        };

        return model.Type switch
        {
            OtpType.TOTP => new Totp(secretKeyBytes, step: model.Period, mode: hashMode, totpSize: model.Digits),
            OtpType.HOTP => new Hotp(secretKeyBytes, mode: hashMode, hotpSize: model.Digits),
            _ => null
        };
    }
    public async Task<Result> GenerateHotpCodeAsync(Guid itemId)
    {
        TwoFactorAuthCodeEntity entity;
        try
        {
            entity = await _twoFactorAuthCodeRepository.GetByIdAsync(itemId);
            if (entity is null || entity.Type != OtpType.HOTP)
            {
                _logger.LogWarning("GenerateHotpCodeAsync failed: Item {ItemId} is not a valid HOTP item.", itemId);
                return Result.Failure(new Error("Otp.NotHotp", _localizer[SharedResources.NotHotp]));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve item {ItemId} for HOTP counter update.", itemId);
            return Result.Failure(new Error("Otp.GetFailed", _localizer[SharedResources.GetFailed]));
        }

        try
        {
            entity.Counter++;
            await _twoFactorAuthCodeRepository.UpdateAsync(entity);

            _logger.LogInformation("Successfully incremented HOTP counter for item {ItemId} to {NewCounter}", entity.Id, entity.Counter);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update HOTP counter for item {ItemId}", itemId);
            return Result.Failure(new Error("Otp.UpdateFailed", _localizer[SharedResources.HOTPCounterUpdateFailed]));
        }
    }

    private async Task InvokeOnTickAsync()
    {
        if (OnTick == null) return;

        var handlers = OnTick.GetInvocationList();
        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Func<Task> taskHandler)
                {
                    await taskHandler();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An 'OnTick' event subscriber threw an exception.");
            }
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (_cts.IsCancellationRequested) return;

        _logger.LogInformation("Disposing OtpService...");

        _notificationToken?.Dispose();
        _cts.Cancel();

        if (_updateLoopTask != null)
        {
            try
            {
                await _updateLoopTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Update loop task was canceled as expected.");
            }
        }

        _cts.Dispose();
        _logger.LogInformation("OtpService disposed.");
    }
}
