namespace SecureVault.Interaction.Api.Features.QrLogin.Contracts;

public interface IQrLoginChannelService
{
    Task<(bool Success, int NewCount, string Role)> RegisterConnectionAsync(string channelId, string connectionId);
    Task<bool> ValidateChannelAsync(string channelId);
    Task MarkChannelAsCompletedAsync(string channelId);
    Task<string?> LeaveChannelAsync(string connectionId);
}
