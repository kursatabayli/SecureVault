using Microsoft.IdentityModel.Tokens;

namespace SecureVault.App.Application.Contracts.Abstractions.Persistence;

public record DpopKey(ECDsaSecurityKey SigningKey, string PublicJwkString);

public interface IDpopKeyService
{
    Task<DpopKey> GetOrCreateDpopKeyAsync();
    Task ClearDpopKeyCacheAsync();
}