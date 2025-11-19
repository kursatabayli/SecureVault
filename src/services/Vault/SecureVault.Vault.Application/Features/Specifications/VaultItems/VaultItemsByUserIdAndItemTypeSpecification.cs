using SecureVault.Vault.Application.Contracts.Specifications;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Features.Specifications.VaultItems;

internal class VaultItemsByUserIdAndItemTypeSpecification : BaseSpecification<VaultItem, VaultItemResult>
{
    public VaultItemsByUserIdAndItemTypeSpecification(Guid userId, ItemType itemType)
    {
        Criteria = item =>
            item.UserId == userId &&
            item.ItemType == itemType &&
            !item.IsDeleted;

        Projection = item => new VaultItemResult
        {
            Id = item.Id,
            ItemType = item.ItemType,
            EncryptedData = item.EncryptedData,
            Version = item.Version,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
