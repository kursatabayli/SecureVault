using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Services.Service.Implementations
{
    public class BouncyCastleCryptoService : IBouncyCastleCryptoService
    {
        private readonly ECDomainParameters _domainParams;
        private readonly IHashService _hashService;
        public BouncyCastleCryptoService(IHashService hashService)
        {
            X9ECParameters curveParams = ECNamedCurveTable.GetByName("secp256k1");
            _domainParams = new ECDomainParameters(curveParams.Curve, curveParams.G, curveParams.N, curveParams.H, curveParams.GetSeed());
            _hashService = hashService;
        }

        public (byte[] Salt, byte[] PublicKey) GenerateValidKeyPair(string password)
        {
            byte[] privateKeyBytes;
            BigInteger privateKeyInt;
            byte[] saltBytes;
            do
            {
                saltBytes = _hashService.GenerateSalt();
                var masterSecret = _hashService.CreateMasterSecret(password, saltBytes);
                privateKeyBytes = _hashService.GetPrivateKeyForAuth(masterSecret, saltBytes);
                privateKeyInt = new BigInteger(1, privateKeyBytes);
            }
            while (privateKeyInt.SignValue <= 0 || privateKeyInt.CompareTo(_domainParams.N) >= 0);

            var q = _domainParams.G.Multiply(privateKeyInt);
            var publicKeyBytes = q.GetEncoded(false).Skip(1).ToArray();

            return (saltBytes, publicKeyBytes);
        }

        public string SignHash(byte[] hash, byte[] privateKey)
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

            return Convert.ToHexString(signature).ToLower();
        }
    }
}
