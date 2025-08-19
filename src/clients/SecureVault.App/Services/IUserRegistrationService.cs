using SecureVault.App.Services.Models.RecoveryKeyModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services
{
    public interface IUserRegistrationService
    {
        Task<Result> RegisterAndBackupAsync(RegisterUserModel userModel, string password, RecoveryKeyModel recoveryKey);
    }
}
