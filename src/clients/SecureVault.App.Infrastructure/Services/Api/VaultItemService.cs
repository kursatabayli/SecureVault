using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.VaultItem;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api;

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
        _logger.LogInformation("Attempting to get full user vault from API...");
        try
        {
            var vaultItems = await _secureVaultApi.GetUserVaultAsync(cancellationToken);
            _logger.LogInformation("Successfully retrieved {Count} vault items (full vault).", vaultItems.Count);
            return Result<IReadOnlyCollection<VaultItemDto>>.Success(vaultItems);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Get user vault failed: Circuit Breaker is open.");
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Get user vault failed: API error. Status Code: {StatusCode}", ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Api.RequestFailed", "Failed to load data."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Get user vault failed: Network error.");
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get user vault failed: Unexpected error.");
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemsByItemTypeAsync(ItemType itemType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to get vault items by type: {ItemType}", itemType);
        try
        {
            var encryptedItems = await _secureVaultApi.GetVaultItemsByItemTypeAsync(itemType, cancellationToken);
            _logger.LogInformation("Successfully retrieved {Count} vault items for type: {ItemType}", encryptedItems.Count, itemType);
            return Result<IReadOnlyCollection<VaultItemDto>>.Success(encryptedItems);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Get vault items by type failed: Circuit Breaker is open. Type: {ItemType}", itemType);
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Get vault items by type failed: API error. Type: {ItemType}, Status Code: {StatusCode}", itemType, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Api.RequestFailed", "Failed to load data."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Get vault items by type failed: Network error. Type: {ItemType}", itemType);
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get vault items by type failed: Unexpected error. Type: {ItemType}", itemType);
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result> CreateVaultItemAsync(CreateVaultItemDto createVaultItemDto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to create vault item: {ItemId}, Type: {ItemType}", createVaultItemDto.Id, createVaultItemDto.ItemType);
        try
        {
            var response = await _secureVaultApi.CreateVaultItemAsync(createVaultItemDto, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created vault item: {ItemId}", createVaultItemDto.Id);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.CreationFailed", "Failed to create item.");
            _logger.LogWarning("Failed to create vault item {ItemId} (API business logic error): {ErrorCode} - {ErrorMessage}", createVaultItemDto.Id, error.Code, error.Message);
            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Create vault item failed: Circuit Breaker is open. ItemId: {ItemId}", createVaultItemDto.Id);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Create vault item failed: API error. ItemId: {ItemId}, Status Code: {StatusCode}", createVaultItemDto.Id, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Failed to create item."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Create vault item failed: Network error. ItemId: {ItemId}", createVaultItemDto.Id);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create vault item failed: Unexpected error. ItemId: {ItemId}", createVaultItemDto.Id);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result> UpdateVaultItemAsync(UpdateVaultItemDto updateEncryptedData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update vault item: {ItemId}", updateEncryptedData.Id);
        try
        {
            var response = await _secureVaultApi.UpdateVaultItemAsync(updateEncryptedData, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated vault item: {ItemId}", updateEncryptedData.Id);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.UpdateFailed", "Failed to update item.");
            _logger.LogWarning("Failed to update vault item {ItemId} (API business logic error): {ErrorCode} - {ErrorMessage}", updateEncryptedData.Id, error.Code, error.Message);
            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Update vault item failed: Circuit Breaker is open. ItemId: {ItemId}", updateEncryptedData.Id);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Update vault item failed: API error. ItemId: {ItemId}, Status Code: {StatusCode}", updateEncryptedData.Id, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Failed to update item."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Update vault item failed: Network error. ItemId: {ItemId}", updateEncryptedData.Id);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update vault item failed: Unexpected error. ItemId: {ItemId}", updateEncryptedData.Id);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result> DeleteVaultItemAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete vault item: {ItemId}", id);
        try
        {
            var response = await _secureVaultApi.DeleteVaultItemAsync(id, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted vault item: {ItemId}", id);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeletionFailed", "Failed to delete item.");
            _logger.LogWarning("Failed to delete vault item {ItemId} (API business logic error): {ErrorCode} - {ErrorMessage}", id, error.Code, error.Message);
            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Delete vault item failed: Circuit Breaker is open. ItemId: {ItemId}", id);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Delete vault item failed: API error. ItemId: {ItemId}, Status Code: {StatusCode}", id, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Failed to delete item."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Delete vault item failed: Network error. ItemId: {ItemId}", id);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete vault item failed: Unexpected error. ItemId: {ItemId}", id);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result<IReadOnlyCollection<VaultItemDto>>> GetVaultItemByLastSyncTimeAsync(DateTimeOffset lastUpdateTime, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to pull sync vault items. Since: {LastUpdateTime}", lastUpdateTime);
        try
        {
            var vaultItems = await _secureVaultApi.GetVaultItemByLastSyncTimeAsync(lastUpdateTime, cancellationToken);
            _logger.LogInformation("Successfully pulled {Count} vault items for sync.", vaultItems.Count);
            return Result<IReadOnlyCollection<VaultItemDto>>.Success(vaultItems);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Pull sync failed: Circuit Breaker is open. Since: {LastUpdateTime}", lastUpdateTime);
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Pull sync failed: API error. Since: {LastUpdateTime}, Status Code: {StatusCode}", lastUpdateTime, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(error ?? new Error("Api.RequestFailed", "Failed to load data."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Pull sync failed: Network error. Since: {LastUpdateTime}", lastUpdateTime);
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pull sync failed: Unexpected error. Since: {LastUpdateTime}", lastUpdateTime);
            return Result<IReadOnlyCollection<VaultItemDto>>.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    // --- Batch Operations ---
    public async Task<Result> CreateVaultItemsAsync(IList<CreateVaultItemDto> createVaultItemDto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to batch create {ItemCount} vault items...", createVaultItemDto.Count);
        try
        {
            var response = await _secureVaultApi.CreateVaultItemsAsync(createVaultItemDto, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully batch created {ItemCount} vault items.", createVaultItemDto.Count);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.CreationFailed", "Failed to create items.");
            _logger.LogWarning("Failed to batch create {ItemCount} vault items (API business logic error): {ErrorCode} - {ErrorMessage}", createVaultItemDto.Count, error.Code, error.Message);
            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Batch create vault items failed: Circuit Breaker is open. ItemCount: {ItemCount}", createVaultItemDto.Count);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Batch create vault items failed: API error. ItemCount: {ItemCount}, Status Code: {StatusCode}", createVaultItemDto.Count, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Failed to create items."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Batch create vault items failed: Network error. ItemCount: {ItemCount}", createVaultItemDto.Count);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch create vault items failed: Unexpected error. ItemCount: {ItemCount}", createVaultItemDto.Count);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result> UpdateVaultItemsAsync(IList<UpdateVaultItemDto> updateEncryptedData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to batch update {ItemCount} vault items...", updateEncryptedData.Count);
        try
        {
            var response = await _secureVaultApi.UpdateVaultItemsAsync(updateEncryptedData, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully batch updated {ItemCount} vault items.", updateEncryptedData.Count);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.UpdateFailed", "Failed to update items.");
            _logger.LogWarning("Failed to batch update {ItemCount} vault items (API business logic error): {ErrorCode} - {ErrorMessage}", updateEncryptedData.Count, error.Code, error.Message);
            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Batch update vault items failed: Circuit Breaker is open. ItemCount: {ItemCount}", updateEncryptedData.Count);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Batch update vault items failed: API error. ItemCount: {ItemCount}, Status Code: {StatusCode}", updateEncryptedData.Count, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Failed to update items."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Batch update vault items failed: Network error. ItemCount: {ItemCount}", updateEncryptedData.Count);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch update vault items failed: Unexpected error. ItemCount: {ItemCount}", updateEncryptedData.Count);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }

    public async Task<Result> DeleteVaultItemsAsync(IList<Guid> ids, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to batch delete {ItemCount} vault items...", ids.Count);
        try
        {
            var response = await _secureVaultApi.DeleteVaultItemsAsync(ids, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully batch deleted {ItemCount} vault items.", ids.Count);
                return Result.Success();
            }

            var error = await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeletionFailed", "Failed to delete items.");
            _logger.LogWarning("Failed to batch delete {ItemCount} vault items (API business logic error): {ErrorCode} - {ErrorMessage}", ids.Count, error.Code, error.Message);
            return Result.Failure(error);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Batch delete vault items failed: Circuit Breaker is open. ItemCount: {ItemCount}", ids.Count);
            return Result.Failure(new Error("Service.Unavailable", "Service is temporarily unavailable."));
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Batch delete vault items failed: API error. ItemCount: {ItemCount}, Status Code: {StatusCode}", ids.Count, ex.StatusCode);
            var error = await ex.GetContentAsAsync<Error>();
            return Result.Failure(error ?? new Error("Api.RequestFailed", "Failed to delete items."));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Batch delete vault items failed: Network error. ItemCount: {ItemCount}", ids.Count);
            return Result.Failure(new Error("Service.ConnectionError", "Could not connect to the server."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch delete vault items failed: Unexpected error. ItemCount: {ItemCount}", ids.Count);
            return Result.Failure(new Error("Client.UnexpectedError", "An unexpected error occurred."));
        }
    }
}
