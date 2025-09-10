namespace SecureVault.Shared.Contracts.Events
{
    public abstract record BaseIntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreationDate { get; } = DateTime.UtcNow;
    }
}
