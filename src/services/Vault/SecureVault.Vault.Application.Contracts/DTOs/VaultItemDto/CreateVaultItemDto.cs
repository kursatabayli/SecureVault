using SecureVault.Vault.Domain.Enums;

namespace SecureVault.Vault.Application.Contracts.DTOs.VaultItemDto
{
    public record CreateVaultItemDto(Guid Id, ItemType ItemType, byte[] EncryptedData, DateTimeOffset CreatedAt);
}
