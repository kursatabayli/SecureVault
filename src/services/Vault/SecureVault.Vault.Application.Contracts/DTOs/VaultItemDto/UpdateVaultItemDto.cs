namespace SecureVault.Vault.Application.Contracts.DTOs.VaultItemDto;

public record UpdateVaultItemDto(Guid Id, byte[] EncryptedData, DateTimeOffset UpdatedAt);
