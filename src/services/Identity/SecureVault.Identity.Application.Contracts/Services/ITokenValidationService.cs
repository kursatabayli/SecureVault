using SecureVault.Identity.Domain.Entities;
using SecureVault.Shared.Result;

namespace SecureVault.Identity.Application.Contracts.Services
{
    public interface ITokenValidationService
    {
        Task<Result<UserSession>> ValidateAndGetSessionAsync(string? accessToken, string? refreshToken, string? proofJkt = null);
    }
}
