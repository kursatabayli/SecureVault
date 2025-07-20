namespace SecureVault.App.Services.Service.Contracts
{
    public interface IHashService
    {
        byte[] GenerateSalt();
        byte[] CreateMasterSecret(string password, byte[] salt);
        byte[] GetPrivateKeyForAuth(byte[] masterSecret, byte[] salt);
        byte[] GetEncryptionKeyForData(byte[] masterSecret, byte[] salt);

        //bool VerifyHash(string requestItem, string storedHash, string storedSalt);
    }
}
