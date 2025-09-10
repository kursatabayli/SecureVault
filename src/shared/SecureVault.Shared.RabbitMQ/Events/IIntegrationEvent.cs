namespace SecureVault.Shared.Contracts.Events
{
    public interface IIntegrationEvent
    {
        public Guid Id { get; }
        public DateTime CreationDate { get; }
    }
}
