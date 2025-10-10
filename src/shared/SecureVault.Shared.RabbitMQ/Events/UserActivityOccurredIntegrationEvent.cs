namespace SecureVault.Shared.Contracts.Events
{
    public record UserActivityOccurredIntegrationEvent(Guid UserId, string OriginDeviceId) : BaseIntegrationEvent;
}
