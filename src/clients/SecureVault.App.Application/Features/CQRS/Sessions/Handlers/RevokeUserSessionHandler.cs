using MediatR;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Interaction;
using SecureVault.App.Application.Features.CQRS.Sessions.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Handlers
{
    public class RevokeUserSessionCommandHandler : IRequestHandler<RevokeUserSessionCommand, Result>
    {
        private readonly IUserSessionService _userSessionService;
        private readonly IInteractionConnectionService _interactionConnectionService;

        public RevokeUserSessionCommandHandler(IUserSessionService userSessionService, IInteractionConnectionService interactionConnectionService)
        {
            _userSessionService = userSessionService;
            _interactionConnectionService = interactionConnectionService;
        }

        public async Task<Result> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
        {
            if (request.IsActiveNow)
                await _interactionConnectionService.NotifyUserSessionRevokedAsync(request.SessionId.ToString(), cancellationToken);

            var result = await _userSessionService.LogoutAnyWhereAsync(request.SessionId, cancellationToken);
            return result;
        }
    }
}
