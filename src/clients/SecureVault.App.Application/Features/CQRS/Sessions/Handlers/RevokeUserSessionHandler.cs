using MediatR;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Features.CQRS.Sessions.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Handlers
{
    public class RevokeUserSessionCommandHandler : IRequestHandler<RevokeUserSessionCommand, Result>
    {
        private readonly IUserSessionService _userSessionService;

        public RevokeUserSessionCommandHandler(IUserSessionService userSessionService)
        {
            _userSessionService = userSessionService;
        }

        public async Task<Result> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
        {
            return await _userSessionService.LogoutAnyWhereAsync(request.SessionId, cancellationToken);
        }
    }
}
