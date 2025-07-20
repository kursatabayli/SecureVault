namespace SecureVault.App.Services.Service.Contracts
{
    public interface IBouncyCastleCryptoService
    {
        (byte[] Salt, byte[] PublicKey) GenerateValidKeyPair(string password);
        string SignHash(byte[] hash, byte[] privateKey);
    }
}
