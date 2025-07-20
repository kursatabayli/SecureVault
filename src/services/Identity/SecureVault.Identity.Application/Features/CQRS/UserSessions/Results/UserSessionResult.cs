namespace SecureVault.Identity.Application.Features.CQRS.UserSessions.Results
{
    public class UserSessionResult
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public DeviceDetailResult DeviceDetails { get; init; }
        public string? IpAddress { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public bool IsRevoked { get; init; }
        public DateTimeOffset? LastUsedAt { get; init; }
    }
}
