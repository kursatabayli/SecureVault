namespace SecureVault.App.Application.Contracts.Abstractions.Cryptography
{
    public interface IBouncyCastleCryptoService
    {
        (byte[] Salt, byte[] PublicKey) GenerateValidKeyPair(string password);
        string SignHash(byte[] hash, byte[] privateKey);
    }
}
