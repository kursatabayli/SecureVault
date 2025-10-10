namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin
{
    public interface ISecureChannelManager : IDisposable
    {
        string InitiateKeyExchange();
        void FinalizeKeyExchange(string counterPartyPublicKeyBase64);
        byte[] Encrypt<T>(T data);
        T Decrypt<T>(byte[] encryptedData);
        bool IsSecureChannelEstablished { get; }
    }
}
