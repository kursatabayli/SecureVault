using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OtpNet;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Queries;
using SecureVault.App.Domain.Enums;
using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.App.Resources.Localization;
using SecureVault.Shared.Result;
using OtpType = SecureVault.App.Domain.Enums.OtpType;

namespace SecureVault.App.Services
{
    public class OtpService : IOtpService
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<OtpService> _logger;
        private readonly List<OtpViewModel> _displayItems = [];
        private readonly CancellationTokenSource _cts = new();
        public bool IsLoading { get; private set; } = true;
        public Error? InitializationError { get; private set; }
        public event Func<Task>? OnTick;
        public IReadOnlyList<OtpViewModel> Items => _displayItems.AsReadOnly();
        private bool _isLoopStarted = false;
        private Task? _updateLoopTask;
        public OtpService(IMediator mediator, IMapper mapper, IStringLocalizer<SharedResources> localizer, ILogger<OtpService> logger)
        {
            _mediator = mediator;
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
                await LoadDataAndGenerateInitialCodes();
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

        public OtpViewModel? GetItem(Guid id)
        {
            return _displayItems.FirstOrDefault(i => i.Model.Id == id);
        }

        private async Task LoadDataAndGenerateInitialCodes()
        {
            var result = await _mediator.Send(new GetAllTwoFactorAuthCodeQuery());
            if (result is null)
                return;
            var twoFactorAuthModel = _mapper.Map<List<TwoFactorAuthCodeModel>>(result);
            _displayItems.Clear();
            _displayItems.AddRange(twoFactorAuthModel.Select(model => new OtpViewModel { Model = model }));

            foreach (var item in _displayItems)
            {
                item.OtpGenerator = OtpService.CreateOtpGenerator(item.Model);
                GenerateNewCode(item);
                OtpService.UpdateTimeLeft(item);
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
            else if (item.OtpGenerator is Hotp)
            {
                item.CurrentCode = _localizer[SharedResources.Text_ClickToGenerate];
            }
        }
        private static Otp? CreateOtpGenerator(TwoFactorAuthCodeModel model)
        {
            var secretKeyBytes = Base32Encoding.ToBytes(model.SecretKey);
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
        public async Task GenerateHotpCodeAsync(Guid itemId)
        {
            var item = _displayItems.FirstOrDefault(i => i.Model.Id == itemId);

            if (item is null || item.OtpGenerator is not Hotp hotpGenerator)
            {
                _logger.LogWarning("HOTP code generation requested for a non-HOTP or non-existent item with ID {ItemId}", itemId);
                return;
            }

            try
            {
                item.CurrentCode = hotpGenerator.ComputeHOTP(item.Model.Counter);
                item.HasError = false;

                item.Model.Counter++;

                //var updateResult = await _vaultItemService.UpdateVaultItem(item.Model);
                //if (!updateResult.IsSuccess)
                //{
                //    _logger.LogError("Failed to update HOTP counter for item {ItemId}: {Error}", itemId, updateResult.Error);
                //    item.HasError = true;
                //    item.CurrentCode = _localizer[SharedResources.Text_Error_General];
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating HOTP code for item {ItemId}", itemId);
                item.HasError = true;
                item.CurrentCode = _localizer[SharedResources.Text_Error_General];
            }
            finally
            {
                await InvokeOnTickAsync();
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

            _cts.Cancel();
            if (_updateLoopTask != null)
            {
                await _updateLoopTask;
            }
            _cts.Dispose();
        }
    }
}
