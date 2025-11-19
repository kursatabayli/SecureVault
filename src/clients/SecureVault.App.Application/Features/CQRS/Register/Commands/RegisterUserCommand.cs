using MediatR;
using SecureVault.App.Application.Contracts.DTOs.User;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Register.Commands;

public class RegisterUserCommand : IRequest<Result>
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string PhoneNumber { get; set; }
    public string MnemonicRecoveryKey { get; set; }
}
