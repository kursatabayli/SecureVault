namespace SecureVault.Interaction.Api.Features.QrLogin.Contracts;

public interface IQrLoginChannelService
{
    Task<string> CreateChannelAsync();
    Task<int> GetChannelCountAsync(string channelId);
    Task<bool> ValidateChannelAsync(string channelId);
    Task MarkChannelAsCompletedAsync(string channelId);
    Task<int> JoinChannelAsync(string channelId, string connectionId);
    Task<string?> LeaveChannelAsync(string connectionId);
}
