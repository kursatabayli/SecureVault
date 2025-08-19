using SecureVault.App.Services.Models.RecoveryKeyModels;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface IBip39RecoveryKeyService
    {
        RecoveryKeyModel Generate();
        bool Validate(string mnemonic);
        byte[] BackupEncryptionKey(string mnemonic, byte[] encryptionKey);
    }
}
