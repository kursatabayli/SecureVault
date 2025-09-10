namespace SecureVault.App.Application.Contracts.Abstractions.Cryptography
{
    public interface IBip39RecoveryKeyService
    {
        string Generate();
        bool Validate(string mnemonic);
        byte[] BackupEncryptionKey(string mnemonic, byte[] encryptionKey);
    }
}
