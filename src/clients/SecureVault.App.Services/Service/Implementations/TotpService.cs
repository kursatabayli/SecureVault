using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OtpNet;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Implementations
{
    public class TotpService : ITotpService
    {
        private readonly IVaultItemService<TwoFactorAuthModel> _vaultItemService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<TotpService> _logger;
        private readonly List<TotpViewModel> _displayItems = [];
        private readonly CancellationTokenSource _cts = new();
        public bool IsLoading { get; private set; } = true;
        public Error? InitializationError { get; private set; }
        public event Action? OnTick;
        public IReadOnlyList<TotpViewModel> Items => _displayItems.AsReadOnly();

        public TotpService(IVaultItemService<TwoFactorAuthModel> vaultItemService, IStringLocalizer<SharedResources> localizer, ILogger<TotpService> logger)
        {
            _vaultItemService = vaultItemService;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            if (_displayItems.Any()) return;

            IsLoading = true;
            InitializationError = null;
            OnTick?.Invoke();

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
                OnTick?.Invoke();
            }

            _ = StartUiUpdateLoop();
        }

        public TotpViewModel? GetItem(Guid id)
        {
            return _displayItems.FirstOrDefault(i => i.Model.Id == id);
        }

        private async Task LoadDataAndGenerateInitialCodes()
        {
            var result = await _vaultItemService.GetVaultItemsByItemTypeAsync(ItemType.TwoFactorAuth);
            if (!result.IsSuccess)
            {
                InitializationError = result.Error;
                return;
            }

            _displayItems.AddRange(result.Value.Select(model => new TotpViewModel { Model = model }));

            foreach (var item in _displayItems)
            {
                item.TotpGenerator = CreateTotp(item.Model);
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
                        int previousTimeLeft = item.TimeLeft;
                        UpdateTimeLeft(item);

                        if (item.TimeLeft > previousTimeLeft)
                        {
                            GenerateNewCode(item);
                        }
                    }

                    OnTick?.Invoke();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "TOTP UI update loop failed.");
                }
            }
        }
        private void UpdateTimeLeft(TotpViewModel item)
        {
            if (item.TotpGenerator == null) return;
            item.TimeLeft = item.TotpGenerator.RemainingSeconds();
            item.ProgressValue = ((double)item.TimeLeft / item.Model.Period) * 100;
        }

        private void GenerateNewCode(TotpViewModel item)
        {
            if (item.TotpGenerator == null)
            {
                item.CurrentCode = _localizer[SharedResources.Text_Error_General];
                item.HasError = true;
                return;
            }
            item.HasError = false;
            item.CurrentCode = item.TotpGenerator.ComputeTotp();
        }
        private Totp CreateTotp(TwoFactorAuthModel model)
        {
            var secretKeyBytes = Base32Encoding.ToBytes(model.SecretKey);
            var hashMode = model.Algorithm switch
            {
                OtpAlgorithm.SHA256 => OtpHashMode.Sha256,
                OtpAlgorithm.SHA512 => OtpHashMode.Sha512,
                _ => OtpHashMode.Sha1,
            };
            return new Totp(secretKeyBytes, step: model.Period, mode: hashMode, totpSize: model.Digits);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
