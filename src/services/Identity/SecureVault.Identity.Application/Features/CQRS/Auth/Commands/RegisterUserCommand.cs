using MediatR;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Commands
{
    public record RegisterUserCommand(string Email, byte[] PublicKey, byte[] Salt, UserInfo UserInfo) : IRequest<Result>;
}
