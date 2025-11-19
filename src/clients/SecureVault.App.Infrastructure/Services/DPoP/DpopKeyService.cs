using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using Microsoft.Extensions.Caching.Memory;
using SecureVault.App.Application.Contracts.Abstractions.DPoP;
using Microsoft.Extensions.Logging;

namespace SecureVault.App.Infrastructure.Services.DPoP;

public class DpopKeyService : IDpopKeyService
{
    private readonly IStorageService _secureStorage;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DpopKeyService> _logger;
    private const string _dpopKeyCacheKey = "DpopKeyService.Key";

    public DpopKeyService(IStorageService secureStorage, IMemoryCache cache, ILogger<DpopKeyService> logger)
    {
        _secureStorage = secureStorage;
        _cache = cache;
        _logger = logger;
    }

    public Task<DpopKey> GetOrCreateDpopKeyAsync()
    {
        return _cache.GetOrCreateAsync(_dpopKeyCacheKey, async entry =>
        {
            try
            {
                _logger.LogInformation("DPoP key not found in memory cache. Attempting to retrieve from SecureStorage...");
                string? privateJwkJson = await _secureStorage.GetDPoPKeyAsync();
                JsonWebKey privateJwk;

                if (string.IsNullOrEmpty(privateJwkJson))
                {
                    _logger.LogInformation("No existing DPoP key found in SecureStorage. Generating and storing a new key...");
                    (DpopKey key, string jwkJson) = CreateAndStoreKey();

                    try
                    {
                        await _secureStorage.SetDPoPKeyAsync(jwkJson);
                        _logger.LogInformation("New DPoP private key saved to SecureStorage.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save new DPoP private key to SecureStorage (key will be session-only).");
                    }

                    return key;
                }

                _logger.LogInformation("Existing DPoP key found in SecureStorage. Reconstructing key from JWK...");
                privateJwk = new JsonWebKey(privateJwkJson);
                return CreateDpopKeyFromJwk(privateJwk);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to create or retrieve DPoP key (e.g., corrupted JWK in storage). DPoP proofs cannot be generated.");
                throw;
            }
        });
    }

    public Task ClearDpopKeyCacheAsync()
    {
        _logger.LogInformation("Clearing in-memory DPoP key cache (e.g., during logout).");
        _cache.Remove(_dpopKeyCacheKey);
        return Task.CompletedTask;
    }

    private (DpopKey key, string jwkJson) CreateAndStoreKey()
    {
        _logger.LogDebug("Generating new P-256 ECDSA key for DPoP...");

        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var ecdsaParams = ecdsa.ExportParameters(true);

        var privateJwk = new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
            Crv = "P-256",
            Alg = SecurityAlgorithms.EcdsaSha256,
            KeyId = Guid.NewGuid().ToString(),

            D = Base64UrlEncoder.Encode(ecdsaParams.D),

            X = Base64UrlEncoder.Encode(ecdsaParams.Q.X),
            Y = Base64UrlEncoder.Encode(ecdsaParams.Q.Y)
        };

        var privateJwkJson = JsonSerializer.Serialize(privateJwk);

        var signingKey = new ECDsaSecurityKey(ecdsa) { KeyId = privateJwk.KeyId };

        var publicJwk = new JsonWebKey
        {
            Kty = privateJwk.Kty,
            Crv = privateJwk.Crv,
            Alg = privateJwk.Alg,
            KeyId = privateJwk.KeyId,
            X = privateJwk.X,
            Y = privateJwk.Y
        };
        var publicJwkString = JsonSerializer.Serialize(publicJwk);

        var dpopKey = new DpopKey(signingKey, publicJwkString);

        _logger.LogDebug("New DPoP key generated successfully. KeyId: {KeyId}", privateJwk.KeyId);

        return (dpopKey, privateJwkJson);
    }

    private DpopKey CreateDpopKeyFromJwk(JsonWebKey privateJwk)
    {
        _logger.LogDebug("Reconstructing DPoP key from stored JWK. KeyId: {KeyId}", privateJwk.KeyId);

        var ecdsaParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            D = Base64UrlEncoder.DecodeBytes(privateJwk.D),
            Q = new ECPoint
            {
                X = Base64UrlEncoder.DecodeBytes(privateJwk.X),
                Y = Base64UrlEncoder.DecodeBytes(privateJwk.Y)
            }
        };

        var ecdsa = ECDsa.Create(ecdsaParams);

        var signingKey = new ECDsaSecurityKey(ecdsa) { KeyId = privateJwk.KeyId };

        var publicJwk = new JsonWebKey
        {
            KeyId = privateJwk.KeyId,
            Kty = privateJwk.Kty,
            Alg = privateJwk.Alg,
            Use = "sig",
            Crv = privateJwk.Crv,
            X = privateJwk.X,
            Y = privateJwk.Y,
        };

        var publicJwkString = JsonSerializer.Serialize(publicJwk);

        return new DpopKey(signingKey, publicJwkString);
    }
}