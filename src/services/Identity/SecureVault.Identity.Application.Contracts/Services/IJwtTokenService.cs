using SecureVault.Identity.Domain.Entities;
using System.Security.Claims;

namespace SecureVault.Identity.Application.Contracts.Services
{
    public interface IJwtTokenService
    {
        (string token, string jti, DateTime expiration) GenerateJwtTokenForUser(User user, string? dpopJkt);
        (string token, string jti, DateTime expiration) GenerateRefreshTokenJwt(Guid userId, bool rememberMe, string? dpopJkt);
        ClaimsPrincipal? GetPrincipalFromAccessToken(string token, bool validateLifetime = true);
        ClaimsPrincipal? GetPrincipalFromRefreshToken(string token);
    }
}
