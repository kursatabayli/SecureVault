using Microsoft.Extensions.Logging;
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
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Client.LoadFailed", "Veriler yüklenemedi."));
            }
        }

        public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemsByItemTypeAsync(ItemType itemType, CancellationToken cancellationToken)
        {
            try
            {
                var encryptedItems = await _secureVaultApi.GetVaultItemsByItemTypeAsync(itemType, cancellationToken);
                return Result<IReadOnlyCollection<VaultItemDto>>.Success(encryptedItems);
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Client.LoadFailed", "Veriler yüklenemedi."));
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
            catch (ApiException ex)
            {
                var errorContent = ex.Content;
                System.Diagnostics.Debug.WriteLine($"API'den gelen ham hata içeriği: {errorContent}");
                return Result.Failure(new Error("Client.ApiError", $"API Yanıtı Okunamadı: {errorContent}"));
            }
        }

        public async Task<Result> UpdateVaultItemAsync(UpdateVaultItemDto updateEncryptedData, CancellationToken cancellationToken)
        {
            try
            {
                //var encryptionKey = await GetEncryptionKeyAsync();
                //var encryptedData = _cryptoService.Encrypt(data, encryptionKey); 

                var response = await _secureVaultApi.UpdateVaultItemAsync(updateEncryptedData, cancellationToken);

                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.UpdateFailed", "Öğe güncellenemedi."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Vault item güncellenirken bir hata oluştu. Id: {Id}", updateEncryptedData.Id);
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.UpdateFailed", "Öğe güncellenemedi."));
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
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.DeletionFailed", "Öğe silinemedi."));
            }
        }

        public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemByLastSyncTimeAsync(DateTimeOffset lastUpdateTime, CancellationToken cancellationToken)
        {
            try
            {
                var vaultItems = await _secureVaultApi.GetVaultItemByLastSyncTimeAsync(lastUpdateTime, cancellationToken);
                return Result<IReadOnlyCollection<VaultItemDto>>.Success(vaultItems);
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Client.LoadFailed", "Veriler yüklenemedi."));
            }
        }
    }
}
