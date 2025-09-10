namespace SecureVault.Interaction.Api.Features.QrLogin.Contracts
{
    public interface IQrLoginChannelService
    {
        Task<string> CreateChannelAsync();
        Task<bool> ValidateChannelAsync(string channelId);
        Task MarkChannelAsCompletedAsync(string channelId);
    }
}
