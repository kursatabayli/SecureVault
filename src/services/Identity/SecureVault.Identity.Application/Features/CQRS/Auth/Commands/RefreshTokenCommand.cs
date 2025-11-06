using MediatR;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Commands
{
    public record RefreshTokenCommand(
        string? AccessToken,
        string? RefreshToken,

        string? IpAddress,
        string? UniqueDeviceId,
        string? DeviceName
    ) : IRequest<Result<AuthResponse>>;
}
