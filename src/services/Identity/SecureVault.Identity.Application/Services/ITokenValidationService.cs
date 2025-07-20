using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Services
{
    public interface ITokenValidationService
    {
        Task<Result<(UserSession session, Guid userId)>> ValidateRefreshTokenAndGetSessionAsync(string refreshToken, string? uniqueDeviceId = null);
    }
}
