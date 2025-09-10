using FluentAssertions;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using SecureVault.Identity.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;

namespace SecureVault.Identity.Infrastructure.Tests.Services
{

    public class EcdsaVerificationServiceTests
    {
        private readonly EcdsaVerificationService _service;

        public EcdsaVerificationServiceTests()
        {
            _service = new EcdsaVerificationService();
        }

        private (byte[] publicKey, string signatureHex) CreateValidSignature(string message, out AsymmetricCipherKeyPair keyPair)
        {
            var curve = ECNamedCurveTable.GetByName("secp256k1");
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            var keyGen = new ECKeyPairGenerator();
            keyGen.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));
            keyPair = keyGen.GenerateKeyPair();

            var privateKeyParams = (ECPrivateKeyParameters)keyPair.Private;
            var msgBytes = Encoding.UTF8.GetBytes(message);
            var msgHash = SHA256.HashData(msgBytes);

            var signer = new ECDsaSigner();
            signer.Init(true, privateKeyParams);
            BigInteger[] signatureComponents = signer.GenerateSignature(msgHash);

            var r = signatureComponents[0];
            var s = signatureComponents[1];

            var rBytes = r.ToByteArrayUnsigned();
            var sBytes = s.ToByteArrayUnsigned();

            var signature = new byte[64];
            Array.Copy(rBytes, 0, signature, 32 - rBytes.Length, rBytes.Length);
            Array.Copy(sBytes, 0, signature, 64 - sBytes.Length, sBytes.Length);

            var publicKey = ((ECPublicKeyParameters)keyPair.Public).Q.GetEncoded(false).Skip(1).ToArray();
            return (publicKey, Convert.ToHexString(signature).ToLower());
        }

        [Fact]
        public void VerifySignature_Should_ReturnTrue_ForValidSignature()
        {
            var message = "This is a test message";
            var (publicKey, signatureHex) = CreateValidSignature(message, out _);

            var result = _service.VerifySignature(message, signatureHex, publicKey);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifySignature_Should_ReturnFalse_ForInvalidSignature()
        {
            var message = "This is a test message";
            var (publicKey, _) = CreateValidSignature(message, out _);
            var invalidSignature = new string('0', 128); 

            var result = _service.VerifySignature(message, invalidSignature, publicKey);

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifySignature_Should_ReturnFalse_ForDifferentMessage()
        {
            var originalMessage = "This is a test message";
            var differentMessage = "This is a different message";
            var (publicKey, signatureHex) = CreateValidSignature(originalMessage, out _);

            var result = _service.VerifySignature(differentMessage, signatureHex, publicKey);

            result.Should().BeFalse();
        }
    }

}
