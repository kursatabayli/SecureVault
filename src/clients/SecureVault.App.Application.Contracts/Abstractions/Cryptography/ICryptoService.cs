namespace SecureVault.App.Application.Contracts.Abstractions.Cryptography;

public interface ICryptoService
{
    byte[] Encrypt<T>(T dataToEncrypt, byte[] encryptionKey);
    T Decrypt<T>(byte[] encryptedData, byte[] encryptionKey);
    byte[] EncryptBytes(byte[] plaintextBytes, byte[] encryptionKey);
    byte[] DecryptBytes(byte[] encryptedData, byte[] encryptionKey);
}
