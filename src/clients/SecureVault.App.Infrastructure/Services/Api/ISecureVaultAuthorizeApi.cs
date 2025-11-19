using Refit;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api;

public interface ISecureVaultAuthorizeApi
{
    //USerSessionService
    [Get("/identity/api/UserSession/")]
    Task<List<UserSessionsDto>> GetUserSessionsAsync(CancellationToken cancellationToken);

    [Delete("/identity/api/UserSession/{sessionId}")]
    Task<IApiResponse> LogoutSessionAsync(Guid sessionId, CancellationToken cancellationToken);


    //VaultItemService
    [Get("/vault/api/VaultItem/")]
    Task<IReadOnlyCollection<VaultItemDto>> GetUserVaultAsync(CancellationToken cancellationToken);

    [Get("/vault/api/VaultItem/vault-items/type/{itemType}")]
    Task<IReadOnlyCollection<VaultItemDto>> GetVaultItemsByItemTypeAsync(ItemType itemType, CancellationToken cancellationToken);

    [Post("/vault/api/VaultItem/")]
    Task<IApiResponse> CreateVaultItemAsync([Body] CreateVaultItemDto vaultItemPayload, CancellationToken cancellationToken);

    [Put("/vault/api/VaultItem/")]
    Task<IApiResponse> UpdateVaultItemAsync([Body] UpdateVaultItemDto payload, CancellationToken cancellationToken);

    [Delete("/vault/api/VaultItem/{id}")]
    Task<IApiResponse> DeleteVaultItemAsync(Guid id, CancellationToken cancellationToken);

    [Post("/vault/api/VaultItem/batch")]
    Task<IApiResponse> CreateVaultItemsAsync([Body] IList<CreateVaultItemDto> vaultItemPayload, CancellationToken cancellationToken);

    [Put("/vault/api/VaultItem/batch")]
    Task<IApiResponse> UpdateVaultItemsAsync([Body] IList<UpdateVaultItemDto> payload, CancellationToken cancellationToken);

    [Delete("/vault/api/VaultItem/batch")]
    Task<IApiResponse> DeleteVaultItemsAsync([Body] IList<Guid> id, CancellationToken cancellationToken);

    [Get("/vault/api/VaultItem/sync")]
    Task<IReadOnlyCollection<VaultItemDto>> GetVaultItemByLastSyncTimeAsync([Query] DateTimeOffset lastUpdateTime, CancellationToken cancellationToken);

}
