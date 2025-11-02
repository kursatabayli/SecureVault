using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Sessions.Commands
{
    public class RevokeUserSessionCommand : IRequest<Result>
    {
        public Guid SessionId { get; set; }
        public bool IsActiveNow { get; set; }
    }
}
