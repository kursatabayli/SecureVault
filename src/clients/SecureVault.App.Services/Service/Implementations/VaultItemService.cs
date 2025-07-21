using Microsoft.Extensions.Logging;
using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;
using System.Net.Http.Json;

namespace SecureVault.App.Services.Service.Implementations
{
    public class VaultItemService<T> : IVaultItemService<T> where T : class, IVaultItemData, new()
    {
        private readonly IApiClient _apiClient;
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<VaultItemService<T>> _logger;

        public VaultItemService(IApiClient apiClient, ICryptoService cryptoService, ILogger<VaultItemService<T>> logger)
        {
            _apiClient = apiClient;
            _cryptoService = cryptoService;
            _logger = logger;
        }


        public async Task<Result<IReadOnlyCollection<T>>> GetVaultItemsByItemTypeAsync(ItemType itemType)
        {
            var apiResult = await _apiClient.GetAsync<IReadOnlyCollection<VaultItemModel>>(Endpoints.GetVaultItemsByItemTypeUrl + itemType, ClientTypes.AuthenticatedClient);
            if (apiResult.IsFailure)
                return apiResult.Error;

            var encryptedItems = apiResult.Value;
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

        public async Task<Result> CreateVaultItem(T data, ItemType itemType)
        {
            var encryptedData = await _cryptoService.EncryptAsync(data);
            VaultItemModel vaultItemPayload = new()
            {
                ItemType = itemType,
                EncryptedData = encryptedData
            };

            return await _apiClient.PostAsync(Endpoints.VaultItemBaseUrl, vaultItemPayload, ClientTypes.AuthenticatedClient);
        }

        public async Task<Result> UpdateVaultItem(T data)
        {
            try
            {
                var encryptedData = await _cryptoService.EncryptAsync(data);

                var payload = new { EncryptedData = encryptedData };

                var endpoint = Endpoints.VaultItemBaseUrl + data.Id;

                return await _apiClient.PutAsync(endpoint, payload, ClientTypes.AuthenticatedClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vault item güncellenirken bir hata oluştu. ItemId: {ItemId}", data.Id);
                return Result.Failure(new Error("Client.UpdateFailed", "Öğe güncellenemedi."));
            }
        }
    }
}
