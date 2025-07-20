using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecureVault.Identity.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly JwtSettings _jwtSettings;
        public AuthService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public (string, DateTime) GenerateJwtTokenForUser(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.UserInfo.Name),
                new(ClaimTypes.Surname, user.UserInfo.Surname)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
        }

        public (string token, string jti, DateTime expiration) GenerateRefreshTokenJwt(Guid userId, bool rememberMe)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.RefreshTokenKey));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var jti = Guid.NewGuid().ToString();
            DateTime expiration;
            if (rememberMe)
                expiration = DateTime.UtcNow.AddDays(30);
            else
                expiration = DateTime.UtcNow.AddHours(1);

            Claim[] claims =
            [
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Jti, jti),
            ];

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials);

            var tokenHandler = new JwtSecurityTokenHandler();
            return (tokenHandler.WriteToken(token), jti, expiration);
        }

        public ClaimsPrincipal? GetPrincipalFromAccessToken(string token, bool validateLifetime = true)
        {
            return ValidateToken(token, _jwtSettings.Key, validateLifetime);
        }

        public ClaimsPrincipal? GetPrincipalFromRefreshToken(string token)
        {
            return ValidateToken(token, _jwtSettings.RefreshTokenKey, true);
        }

        private ClaimsPrincipal? ValidateToken(string token, string secretKey, bool validateLifetime)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = validateLifetime,
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
