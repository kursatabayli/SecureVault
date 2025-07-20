using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;
using System.Net.Http.Json;

namespace SecureVault.App.Services.Service.Implementations
{
    public class VaultItemService<T> : IVaultItemService<T> where T : class, IVaultItemData, new()
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICryptoService _cryptoService;

        public VaultItemService(IHttpClientFactory httpClientFactory, ICryptoService cryptoService)
        {
            _httpClientFactory = httpClientFactory;
            _cryptoService = cryptoService;
        }

        private HttpClient CreateClient(ClientTypes clientTypes = ClientTypes.AuthenticatedClient) => _httpClientFactory.CreateClient(clientTypes.ToString());

        public async Task<Result<IReadOnlyCollection<T>>> GetVaultItemsByItemTypeAsync(ItemType itemType)
        {
            try
            {
                var client = CreateClient();
                var response = await client.GetAsync(Endpoints.GetVaultItemsByItemTypeUrl + itemType);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<VaultItemModel>>();
                    var decryptedItems = new List<T>();
                    foreach (var item in result)
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
                            return new Error("DecryptionFailed", $"Failed to decrypt item {item.Id}. Reason: {ex.Message}");
                        }
                    }
                    return Result<IReadOnlyCollection<T>>.Success(decryptedItems);
                }
                else
                {
                    return await response.Content.ReadFromJsonAsync<Error>();
                }
            }
            catch (Exception ex)
            {
                Error error = new("ChallengeRequestFailed", $"An error occurred: {ex.Message}");
                return error;
            }
        }

        public async Task<bool> CreateVaultItem(T Data, ItemType itemType)
        {
            var client = CreateClient();
            var encryptedData = await _cryptoService.EncryptAsync(Data);
            VaultItemModel vaultItem = new()
            {
                ItemType = itemType,
                EncryptedData = encryptedData
            };

            var response = await client.PostAsJsonAsync(Endpoints.VaultItemBaseUrl, vaultItem);
            return response.IsSuccessStatusCode;
        }
    }
}
