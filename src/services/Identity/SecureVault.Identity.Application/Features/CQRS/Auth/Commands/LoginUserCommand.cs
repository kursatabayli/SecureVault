using MediatR;
using SecureVault.Identity.Application.Contracts.DTOs.AuthDto;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Commands
{
    public record LoginUserCommand(
        string Email,
        string Signature,
        bool RememberMe,
        string? UniqueDeviceId,
        string? DeviceName,
        string? DeviceModel,
        string? DeviceManufacturer,
        string? OperatingSystem,
        string? IpAddress)
        : IRequest<Result<AuthResponse>>;
}
