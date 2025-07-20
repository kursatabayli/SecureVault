using MediatR;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.Auth.Commands
{
    public record VerifyLoginSignatureCommand(string Email, string Signature) : IRequest<Result<User>>;
}
