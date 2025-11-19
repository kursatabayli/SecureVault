using MediatR;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Passwords.Commands;

public class CreatePasswordCommand : IRequest<Result>
{
    public string SiteName { get; set; }
    public string SiteUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string? Notes { get; set; }
}
