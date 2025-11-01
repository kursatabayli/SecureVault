using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class VaultItemService : IVaultItemService
    {
        private readonly ISecureVaultAuthorizeApi _secureVaultApi;
        private readonly ILogger<VaultItemService> _logger;

        public VaultItemService(ISecureVaultAuthorizeApi secureVaultApi, ILogger<VaultItemService> logger)
        {
            _secureVaultApi = secureVaultApi;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetUserVaultAsync(CancellationToken cancellationToken)
        {
            try
            {
                var vaultItems = await _secureVaultApi.GetUserVaultAsync(cancellationToken);
                return Result<IReadOnlyCollection<VaultItemDto>>.Success(vaultItems);
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa verileri alınamadı.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa verileri alınırken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Api.RequestFailed", "Veriler yüklenemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa verileri alınırken ağ hatası oluştu.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa verileri alınırken beklenmedik bir hata oluştu.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemsByItemTypeAsync(ItemType itemType, CancellationToken cancellationToken)
        {
            try
            {
                var encryptedItems = await _secureVaultApi.GetVaultItemsByItemTypeAsync(itemType, cancellationToken);
                return Result<IReadOnlyCollection<VaultItemDto>>.Success(encryptedItems);
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa verileri türe göre alınamadı.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa verileri türe göre alınırken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Api.RequestFailed", "Veriler yüklenemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa verileri türe göre alınırken ağ hatası oluştu.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa verileri türe göre alınırken beklenmedik bir hata oluştu.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result> CreateVaultItemAsync(CreateVaultItemDto createVaultItemDto, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.CreateVaultItemAsync(createVaultItemDto, cancellationToken);
                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.CreationFailed", "Öğe oluşturulamadı."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa öğesi oluşturulamadı.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa öğesi oluşturulurken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Öğe oluşturulamadı."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa öğesi oluşturulurken ağ hatası oluştu.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa öğesi oluşturulurken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result> UpdateVaultItemAsync(UpdateVaultItemDto updateEncryptedData, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.UpdateVaultItemAsync(updateEncryptedData, cancellationToken);
                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.UpdateFailed", "Öğe güncellenemedi."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa öğesi güncellenemedi.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa öğesi güncellenirken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Öğe güncellenemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa öğesi güncellenirken ağ hatası oluştu.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa öğesi güncellenirken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result> DeleteVaultItemAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.DeleteVaultItemAsync(id, cancellationToken);
                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeletionFailed", "Öğe silinemedi."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa öğesi silinemedi.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa öğesi silinirken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Öğe silinemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa öğesi silinirken ağ hatası oluştu.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa öğesi silinirken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemByLastSyncTimeAsync(DateTimeOffset lastUpdateTime, CancellationToken cancellationToken)
        {
            try
            {
                var vaultItems = await _secureVaultApi.GetVaultItemByLastSyncTimeAsync(lastUpdateTime, cancellationToken);
                return Result<IReadOnlyCollection<VaultItemDto>>.Success(vaultItems);
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Senkronizasyon için veriler alınamadı.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Senkronizasyon için veri alınırken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Api.RequestFailed", "Veriler yüklenemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Senkronizasyon için veri alınırken ağ hatası oluştu.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Senkronizasyon için veri alınırken beklenmedik bir hata oluştu.");
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result> CreateVaultItemsAsync(IList<CreateVaultItemDto> createVaultItemDto, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.CreateVaultItemsAsync(createVaultItemDto, cancellationToken);
                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.CreationFailed", "Öğeler oluşturulamadı."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa öğeleri oluşturulamadı.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa öğeleri oluşturulurken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Öğeler oluşturulamadı."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa öğeleri oluşturulurken ağ hatası oluştu.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa öğeleri oluşturulurken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result> UpdateVaultItemsAsync(IList<UpdateVaultItemDto> UpdateEncryptedData, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.UpdateVaultItemsAsync(UpdateEncryptedData, cancellationToken);
                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.UpdateFailed", "Öğeler güncellenemedi."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa öğeleri güncellenemedi.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa öğeleri güncellenirken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Öğeler güncellenemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa öğeleri güncellenirken ağ hatası oluştu.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa öğeleri güncellenirken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }

        }

        public async Task<Result> DeleteVaultItemsAsync(IList<Guid> ids, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.DeleteVaultItemsAsync(ids, cancellationToken);
                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeletionFailed", "Öğeler silinemedi."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kasa öğeleri silinemedi.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kasa öğeleri silinirken API hatası oluştu.");
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Öğeler silinemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kasa öğeleri silinirken ağ hatası oluştu.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kasa öğeleri silinirken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }
    }
}
