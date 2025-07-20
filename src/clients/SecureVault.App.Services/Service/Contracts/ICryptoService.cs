using Microsoft.Maui.Controls;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface ICryptoService
    {
        Task<byte[]> EncryptAsync<T>(T dataToEncrypt);
        Task<T> DecryptAsync<T>(byte[] encryptedData);

    }
}
