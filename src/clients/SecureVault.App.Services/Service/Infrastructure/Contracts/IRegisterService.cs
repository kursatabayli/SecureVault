using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Infrastructure.Contracts
{
    public interface IRegisterService
    {
        Task<Result?> RegisterAsync(RegisterUserModel registerUserModel);
    }
}
