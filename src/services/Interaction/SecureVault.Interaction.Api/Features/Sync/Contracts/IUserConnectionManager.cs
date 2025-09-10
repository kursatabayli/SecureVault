namespace SecureVault.Interaction.Api.Features.Sync.Contracts
{
    public interface IUserConnectionManager
    {
        void AddConnection(Guid userId, string connectionId);
        Guid? RemoveConnection(string connectionId);
        IReadOnlyList<string> GetConnections(Guid userId);
    }
}
