using MediatR;
using SecureVault.Identity.Application.Features.CQRS.UserSessions.Results;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Features.CQRS.UserSessions.Queries
{
    public record GetAllUserSessionsByUserIdQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<UserSessionResult>>>;
}
