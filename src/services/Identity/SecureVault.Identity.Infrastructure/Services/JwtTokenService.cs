using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecureVault.Identity.Application.Contracts.Services;
using SecureVault.Identity.Domain.Entities;
using SecureVault.Identity.Infrastructure.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SecureVault.Identity.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly ILogger<JwtTokenService> _logger;
    private readonly JwtSettings _jwtSettings;
    public JwtTokenService(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public (string token, string jti, DateTime expiration) GenerateJwtTokenForUser(User user, string? dpopJkt)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.UserInfo.Name),
            new(ClaimTypes.Surname, user.UserInfo.Surname)
        };

        if (!string.IsNullOrEmpty(dpopJkt))
        {
            var cnf = new Dictionary<string, object>
            {
                { "jkt", dpopJkt }
            };
            claims.Add(new Claim("cnf", JsonSerializer.Serialize(cnf), JsonClaimValueTypes.Json));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), jti, token.ValidTo);
    }

    public (string token, string jti, DateTime expiration) GenerateRefreshTokenJwt(Guid userId, bool rememberMe, string? dpopJkt)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.RefreshTokenKey));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString();
        DateTime expiration;
        if (rememberMe)
            expiration = DateTime.UtcNow.AddDays(30);
        else
            expiration = DateTime.UtcNow.AddHours(1);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
        };
        if (!string.IsNullOrEmpty(dpopJkt))
        {
            var cnf = new Dictionary<string, object>
            {
                { "jkt", dpopJkt }
            };
            claims.Add(new Claim("cnf", JsonSerializer.Serialize(cnf), JsonClaimValueTypes.Json));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        var tokenHandler = new JwtSecurityTokenHandler();
        return (tokenHandler.WriteToken(token), jti, expiration);
    }

    public ClaimsPrincipal? GetPrincipalFromAccessToken(string token, bool validateLifetime = true) => ValidateToken(token, _jwtSettings.Key, validateLifetime);

    public ClaimsPrincipal? GetPrincipalFromRefreshToken(string token) => ValidateToken(token, _jwtSettings.RefreshTokenKey, true);

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
                _logger.LogWarning("Token validation failed: Invalid algorithm used.");
                return null;
            }

            return principal;
        }
        catch (SecurityTokenValidationException stvex)
        {
            _logger.LogWarning("Token validation failed (expected rejection): {ValidationFailureReason}", stvex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected internal error occurred during token validation.");
            return null;
        }
    }
}
