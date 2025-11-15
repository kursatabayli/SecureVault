using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using Microsoft.Extensions.Caching.Memory;

namespace SecureVault.App.Infrastructure.Services.Storage;

public class DpopKeyService : IDpopKeyService
{
    private readonly IStorageService _secureStorage;
    private readonly IMemoryCache _cache;
    private const string _dpopKeyCacheKey = "DpopKeyService.Key";

    public DpopKeyService(IStorageService secureStorage, IMemoryCache cache)
    {
        _secureStorage = secureStorage;
        _cache = cache;
    }

    public Task<DpopKey> GetOrCreateDpopKeyAsync()
    {
        return _cache.GetOrCreateAsync(_dpopKeyCacheKey, async entry =>
        {

            string? privateJwkJson = await _secureStorage.GetDPoPKeyAsync();
            JsonWebKey privateJwk;

            if (string.IsNullOrEmpty(privateJwkJson))
            {
                (DpopKey key, string jwkJson) = CreateAndStoreKey();
                await _secureStorage.SetDPoPKeyAsync(jwkJson);

                return key;
            }

            privateJwk = new JsonWebKey(privateJwkJson);
            return CreateDpopKeyFromJwk(privateJwk);
        });
    }

    public Task ClearDpopKeyCacheAsync()
    {
        _cache.Remove(_dpopKeyCacheKey);
        return Task.CompletedTask;
    }

    private (DpopKey key, string jwkJson) CreateAndStoreKey()
    {
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

        return (dpopKey, privateJwkJson);
    }

    private DpopKey CreateDpopKeyFromJwk(JsonWebKey privateJwk)
    {
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