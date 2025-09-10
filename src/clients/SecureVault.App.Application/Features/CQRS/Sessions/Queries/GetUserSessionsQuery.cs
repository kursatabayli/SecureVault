using MediatR;
using SecureVault.App.Application.Features.CQRS.Sessions.Results;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Queries
{
    public class GetUserSessionsQuery : IRequest<Result<List<UserSessionsResult>>>
    {
    }
}
