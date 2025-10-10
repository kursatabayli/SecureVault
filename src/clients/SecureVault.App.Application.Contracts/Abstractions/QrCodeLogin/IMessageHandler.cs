namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin
{
    public interface IMessageHandler
    {
        string MessageType { get; }
        Task HandleAsync(IQrLoginContext context, string payload);
    }
}
