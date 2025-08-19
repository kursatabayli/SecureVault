namespace SecureVault.App.Services.Service.Application.Contracts
{
    public interface IBouncyCastleCryptoService
    {
        (byte[] Salt, byte[] PublicKey) GenerateValidKeyPair(string password);
        string SignHash(byte[] hash, byte[] privateKey);
    }
}
