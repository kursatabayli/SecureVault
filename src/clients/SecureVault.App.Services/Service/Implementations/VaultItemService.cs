using Microsoft.Extensions.Logging;
using Refit;
using SecureVault.App.Services.APIs;
using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;
using System.Net.Http.Json;

namespace SecureVault.App.Services.Service.Implementations
{
    public class VaultItemService<T> : IVaultItemService<T> where T : class, IVaultItemData, new()
    {
        private readonly ISecureVaultApi _secureVaultApi;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<VaultItemService<T>> _logger;

        public VaultItemService(ISecureVaultApi secureVaultApi, ICryptoService cryptoService, ILogger<VaultItemService<T>> logger)
        {
            _secureVaultApi = secureVaultApi;
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyCollection<T>>> GetVaultItemsByItemTypeAsync(ItemType itemType)
        {
            try
            {
                var encryptedItems = await _secureVaultApi.GetVaultItemsByItemTypeAsync(itemType);

                var decryptedItems = new List<T>();
                foreach (var item in encryptedItems)
                {
                    try
                    {
                        var decryptedData = await _cryptoService.DecryptAsync<T>(item.EncryptedData);
                        decryptedData.Id = item.Id.Value;
                        decryptedData.CreatedAt = item.CreatedAt.Value;
                        decryptedItems.Add(decryptedData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Bir vault item'ın şifresi çözülemedi. ItemId: {ItemId}", item.Id);
                    }
                }
                return decryptedItems;
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result<IReadOnlyCollection<T>>.Failure(error ?? new Error("Client.LoadFailed", "Veriler yüklenemedi."));
            }
        }

        public async Task<Result> CreateVaultItem(T data, ItemType itemType)
        {
            try
            {
                var encryptedData = await _cryptoService.EncryptAsync(data);
                VaultItemModel vaultItemPayload = new()
                {
                    ItemType = itemType,
                    EncryptedData = encryptedData
                };

                var response = await _secureVaultApi.CreateVaultItemAsync(vaultItemPayload);

                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.CreationFailed", "Öğe oluşturulamadı."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.CreationFailed", "Öğe oluşturulamadı."));
            }
        }

        public async Task<Result> UpdateVaultItem(T data)
        {
            try
            {
                var encryptedData = await _cryptoService.EncryptAsync(data);
                var payload = new UpdateEncryptedData { EncryptedData = encryptedData };

                var response = await _secureVaultApi.UpdateVaultItemAsync(data.Id, payload);

                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.UpdateFailed", "Öğe güncellenemedi."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Vault item güncellenirken bir hata oluştu. ItemId: {ItemId}", data.Id);
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.UpdateFailed", "Öğe güncellenemedi."));
            }
        }
    }
}
