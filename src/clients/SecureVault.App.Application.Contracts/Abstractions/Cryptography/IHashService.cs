namespace SecureVault.App.Application.Contracts.Abstractions.Cryptography;

public interface IHashService
{
    byte[] GenerateSalt();
    byte[] CreateMasterSecret(string password, byte[] salt);
    byte[] GetPrivateKeyForAuth(byte[] masterSecret, byte[] salt);
    byte[] GetEncryptionKeyForData(byte[] masterSecret, byte[] salt);

    //bool VerifyHash(string requestItem, string storedHash, string storedSalt);
}
