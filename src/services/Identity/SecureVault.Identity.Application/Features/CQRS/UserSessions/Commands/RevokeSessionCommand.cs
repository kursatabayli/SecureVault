using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.UserSessions.Commands
{
    public record RevokeSessionCommand(Guid SessionId, Guid CurrentUserId) : IRequest<Result>;
}
