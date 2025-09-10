namespace SecureVault.Shared.Contracts.Events
{
    public record UserActivityOccurredIntegrationEvent(Guid UserId) : BaseIntegrationEvent;
}
