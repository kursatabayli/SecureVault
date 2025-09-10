using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Commands
{
    public class LogoutCommand : IRequest<Result>;
}
