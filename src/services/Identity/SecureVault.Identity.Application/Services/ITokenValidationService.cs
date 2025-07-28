using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Services
{
    public interface ITokenValidationService
    {
        Task<Result<UserSession>> ValidateAndGetSessionAsync(string? accessToken, string? refreshToken, string? uniqueDeviceId = null);
    }
}
