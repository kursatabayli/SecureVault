using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;

namespace SecureVault.App.Application.Services;

public class BouncyCastleCryptoService : IBouncyCastleCryptoService
{
    private readonly ECDomainParameters _domainParams;
    private readonly IHashService _hashService;
    private readonly ILogger<BouncyCastleCryptoService> _logger;
    public BouncyCastleCryptoService(IHashService hashService, ILogger<BouncyCastleCryptoService> logger)
    {
        X9ECParameters curveParams = ECNamedCurveTable.GetByName("secp256k1");
        _domainParams = new ECDomainParameters(curveParams.Curve, curveParams.G, curveParams.N, curveParams.H, curveParams.GetSeed());
        _hashService = hashService;
        _logger = logger;
    }

    public (byte[] Salt, byte[] PublicKey) GenerateValidKeyPair(string password)
    {
        _logger.LogInformation("Starting ECDSA key pair generation (secp256k1)...");

        try
        {
            byte[] privateKeyBytes;
            BigInteger privateKeyInt;
            byte[] saltBytes;
            int iterationCount = 0;

            do
            {
                iterationCount++;
                saltBytes = _hashService.GenerateSalt();
                var masterSecret = _hashService.CreateMasterSecret(password, saltBytes);
                privateKeyBytes = _hashService.GetPrivateKeyForAuth(masterSecret, saltBytes);
                privateKeyInt = new BigInteger(1, privateKeyBytes);
            }
            while (privateKeyInt.SignValue <= 0 || privateKeyInt.CompareTo(_domainParams.N) >= 0);

            if (iterationCount > 1)
            {
                _logger.LogWarning("Key generation required {Count} attempts due to domain constraint check.", iterationCount);
            }

            var q = _domainParams.G.Multiply(privateKeyInt);
            var publicKeyBytes = q.GetEncoded(false).Skip(1).ToArray();

            _logger.LogInformation("ECDSA key pair generated successfully.");

            return (saltBytes, publicKeyBytes);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "CRITICAL: Failed to generate ECDSA key pair.");
            throw;
        }
    }

    public string SignHash(byte[] hash, byte[] privateKey)
    {
        _logger.LogDebug("Starting ECDSA hash signing...");

        try
        {
            var privateKeyParams = new ECPrivateKeyParameters(new BigInteger(1, privateKey), _domainParams);

            var signer = new ECDsaSigner();
            signer.Init(true, privateKeyParams);

            BigInteger[] signatureComponents = signer.GenerateSignature(hash);
            var r = signatureComponents[0];
            var s = signatureComponents[1];

            var rBytes = r.ToByteArrayUnsigned();
            var sBytes = s.ToByteArrayUnsigned();

            var signature = new byte[64];
            Array.Copy(rBytes, 0, signature, 32 - rBytes.Length, rBytes.Length);
            Array.Copy(sBytes, 0, signature, 64 - sBytes.Length, sBytes.Length);

            var signatureHex = Convert.ToHexString(signature).ToLower();

            _logger.LogDebug("Hash signed successfully. Signature Hex: {SignaturePrefix}...", signatureHex.Substring(0, 10));

            return signatureHex;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign hash with ECDSA.");
            throw;
        }
    }
}
