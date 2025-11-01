using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OtpNet;
using Realms;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Queries;
using SecureVault.App.Domain.Entities;
using SecureVault.App.Domain.Enums;
using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.App.Resources.Localization;
using SecureVault.Shared.Result;
using OtpType = SecureVault.App.Domain.Enums.OtpType;

namespace SecureVault.App.Services
{
    public class OtpService : IOtpService, IDisposable
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
            IsLoading = true;
            InitializationError = null;
            await InvokeOnTickAsync();

            try
            {
                _liveCollection = await _twoFactorAuthCodeRepository.GetLiveCollectionAsync();

                _notificationToken = _liveCollection.SubscribeForNotifications(OnDataChanged);

                LoadDataAndGenerateInitialCodes(_liveCollection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOTP verileri yüklenirken beklenmedik bir hata oluştu.");
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
            try
            {
                LoadDataAndGenerateInitialCodes(sender);
                await InvokeOnTickAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Realm veri değişikliği işlenirken hata oluştu.");
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
        }

        private async Task StartUiUpdateLoop()
        {
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
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "TOTP UI update loop failed.");
                }
            }
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
        private static Otp? CreateOtpGenerator(TwoFactorAuthCodeModel model)
        {
            var secretKeyBytes = Base32Encoding.ToBytes(model.SecretKey);
            if (secretKeyBytes == null || secretKeyBytes.Length == 0)
            {
                Console.WriteLine($"ID'si {model.Id} olan TOTP kaydının secretKey'i boş veya geçersiz. Bu kayıt atlanıyor.");
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
            try
            {
                var entity = await _twoFactorAuthCodeRepository.GetByIdAsync(itemId);
                if (entity is null || entity.Type != OtpType.HOTP)
                {
                    return Result.Failure(new Error("Otp.NotHotp", "Öğe HOTP değil."));
                }

                entity.Counter++;
                await _twoFactorAuthCodeRepository.UpdateAsync(entity);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HOTP sayacı güncellenirken hata oluştu: {ItemId}", itemId);
                return Result.Failure(new Error("Otp.UpdateFailed", ex.Message));
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
                    _logger.LogError(ex, "OnTick abonesi hata fırlattı.");
                }
            }
        }
        public async ValueTask DisposeAsync()
        {
            if (_cts.IsCancellationRequested) return;
            _notificationToken?.Dispose();
            _cts.Cancel();
            if (_updateLoopTask != null)
            {
                await _updateLoopTask;
            }
            _cts.Dispose();
        }
        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }
    }
}
