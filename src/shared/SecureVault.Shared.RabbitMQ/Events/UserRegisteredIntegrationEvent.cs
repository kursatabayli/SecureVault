namespace SecureVault.Shared.Contracts.Events
{
    public record UserRegisteredIntegrationEvent(Guid UserId) : BaseIntegrationEvent;
}
