using SecureVault.App.Application.Contracts.DTOs.Register;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Contracts.Abstractions.Api
{
    public interface IRegisterService
    {
        Task<Result?> RegisterAsync(RegisterUserDto registerUserDto, CancellationToken cancellationToken);
    }
}
