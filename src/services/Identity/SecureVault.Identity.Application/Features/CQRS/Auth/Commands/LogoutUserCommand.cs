using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Commands
{
    public record LogoutUserCommand(string RefreshToken) : IRequest<Result>;
}
