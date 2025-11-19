using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Commands;

public class LoginCommand : IRequest<Result>
{
    public string Email { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}
