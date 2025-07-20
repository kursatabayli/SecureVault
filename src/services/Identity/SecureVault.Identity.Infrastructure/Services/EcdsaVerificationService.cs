using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using SecureVault.Identity.Application.Contracts.Services;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.Identity.Infrastructure.Services
{
    public class EcdsaVerificationService : IEcdsaVerificationService
    {
        private readonly ECDomainParameters _domainParams;

        public EcdsaVerificationService()
        {
            X9ECParameters curveParams = ECNamedCurveTable.GetByName("secp256k1");
            _domainParams = new ECDomainParameters(curveParams.Curve, curveParams.G, curveParams.N, curveParams.H);
        }
        public bool VerifySignature(string message, string signatureHex, byte[] publicKey)
        {
            try
            {
                var encodedPoint = new byte[publicKey.Length + 1];
                encodedPoint[0] = 4;
                Array.Copy(publicKey, 0, encodedPoint, 1, publicKey.Length);
                var q = _domainParams.Curve.DecodePoint(encodedPoint);
                var publicKeyParams = new ECPublicKeyParameters(q, _domainParams);

                var signatureBytes = Convert.FromHexString(signatureHex);
                var rBytes = new byte[32];
                var sBytes = new byte[32];
                Array.Copy(signatureBytes, 0, rBytes, 0, 32);
                Array.Copy(signatureBytes, 32, sBytes, 0, 32);

                var r = new BigInteger(1, rBytes);
                var s = new BigInteger(1, sBytes);

                var msgBytes = Encoding.UTF8.GetBytes(message);
                var msgHash = SHA256.HashData(msgBytes);

                var verifier = new ECDsaSigner();
                verifier.Init(false, publicKeyParams);

                return verifier.VerifySignature(msgHash, r, s);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
