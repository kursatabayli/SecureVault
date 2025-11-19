namespace SecureVault.App.Application.Features.CQRS.Sessions.Results;

public class UserSessionsResult
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string TokenIdentifier { get; set; }
    public DeviceDetailResult DeviceDetails { get; init; }
    public string? IpAddress { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? LastUsedAt { get; init; }
}
