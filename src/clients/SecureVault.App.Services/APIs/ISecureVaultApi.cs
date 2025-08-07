using Refit;
using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Models.SessionModels;
using SecureVault.App.Services.Models.VaultItemModels;

namespace SecureVault.App.Services.APIs
{
    public interface ISecureVaultApi
    {
        //AuthService
        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Register/")]
        Task<IApiResponse> RegisterAsync([Body] RegisterUserModel registerUserDto);

        [Headers("X-Anonymous: true")]
        [Get("/identity/api/Auth/challenge/{email}")]
        Task<ChallengeModel> GetChallengeAsync(string email);

        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Auth/login")]
        Task<AuthResponseModel> LoginAsync([Query] bool rememberMe, [Body] LoginCredentialsModel loginCredentials);

        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Auth/refresh/")]
        Task<AuthResponseModel> RefreshTokenAsync([Header("X-Refresh-Token")] string refreshToken);

        [Headers("X-Anonymous: true")]
        [Post("/identity/api/Auth/logout/")]
        Task<IApiResponse> LogoutAsync([Header("X-Refresh-Token")] string refreshToken);


        //USerSessionService
        [Get("/identity/api/UserSession/")]
        Task<List<UserSessionsModel>> GetUserSessionsAsync();

        [Delete("/identity/api/UserSession/{sessionId}")]
        Task<IApiResponse> LogoutSessionAsync(Guid sessionId);


        //VaultItemService
        [Get("/vault/api/VaultItem/vault-items/type/{itemType}")]
        Task<IReadOnlyCollection<VaultItemModel>> GetVaultItemsByItemTypeAsync(ItemType itemType);

        [Post("/vault/api/VaultItem/")]
        Task<IApiResponse> CreateVaultItemAsync([Body] VaultItemModel vaultItemPayload);

        [Put("/vault/api/VaultItem/{id}")]
        Task<IApiResponse> UpdateVaultItemAsync(Guid id, [Body] UpdateEncryptedData payload);
    }
}
