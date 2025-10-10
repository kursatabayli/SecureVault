namespace SecureVault.Interaction.Api.Features.Sync.Contracts
{
    public interface IUserConnectionManager
    {
        void AddConnection(Guid userId, string connectionId, string deviceId);
        Guid? RemoveConnection(string connectionId);
        IReadOnlyList<string> GetConnections(Guid userId, string? deviceIdToExclude = null);
    }
}
