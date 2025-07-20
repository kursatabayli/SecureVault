using SecureVault.Identity.Domain.Entities;
using System.Security.Claims;

namespace SecureVault.Identity.Application.Contracts.Services
{
    public interface IAuthService
    {
        (string, DateTime) GenerateJwtTokenForUser(User user);
        (string token, string jti, DateTime expiration) GenerateRefreshTokenJwt(Guid userId, bool rememberMe);
        ClaimsPrincipal? GetPrincipalFromAccessToken(string token, bool validateLifetime = true);
        ClaimsPrincipal? GetPrincipalFromRefreshToken(string token);
    }
}
