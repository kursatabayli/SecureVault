using SecureVault.Vault.Application.Contracts.Specifications;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Features.Specifications.VaultItems
{
    internal class VaultItemsByLastSyncTimeSpecification : BaseSpecification<VaultItem, GetAllUserVaultItemsByLastSyncTimeResult>
    {
        public VaultItemsByLastSyncTimeSpecification(Guid userId, DateTimeOffset lastSyncedTime, string deviceId)
        {
            Criteria = item => item.UserId == userId && item.UpdatedAt > lastSyncedTime && item.LastUpdatedByDeviceId != deviceId;

            Projection = item => new GetAllUserVaultItemsByLastSyncTimeResult
            {
                Id = item.Id,
                ItemType = item.ItemType,
                EncryptedData = item.EncryptedData,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Version = item.Version,
                IsDeleted = item.IsDeleted,
            };
        }
    }
}
