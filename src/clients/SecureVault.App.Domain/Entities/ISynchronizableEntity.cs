namespace SecureVault.App.Domain.Entities
{
    public interface ISynchronizableEntity
    {
        Guid Id { get; }
        int Version { get; }
        bool IsSynced { get; }
        bool IsDeleted { get; }
        DateTimeOffset UpdatedAt { get; }

        void MarkAsSynced();
        void MarkAsDeleted();
    }
}
