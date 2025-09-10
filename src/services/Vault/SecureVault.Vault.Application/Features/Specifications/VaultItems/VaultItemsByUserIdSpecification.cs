using SecureVault.Vault.Application.Contracts.Specifications;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Domain.Entities;

namespace SecureVault.Vault.Application.Features.Specifications.VaultItems
{
    public class VaultItemsByUserIdSpecification : BaseSpecification<VaultItem, VaultItemResult>
    {
        public VaultItemsByUserIdSpecification(Guid userId)
        {
            Criteria = item => item.UserId == userId && !item.IsDeleted;

            Projection = item => new VaultItemResult
            {
                Id = item.Id,
                ItemType = item.ItemType,
                EncryptedData = item.EncryptedData,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Version = item.Version
            };
        }
    }
}
