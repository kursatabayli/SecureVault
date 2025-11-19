using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SecureVault.App.Application.Contracts.Abstractions.DPoP;

namespace SecureVault.App.Infrastructure.Services.DPoP;

public class DpopProofService : IDpopProofService
{
  private readonly IDpopKeyService _dpopKeyService;
  private readonly JwtSecurityTokenHandler _tokenHandler;
  private readonly ILogger<DpopProofService> _logger;

  public DpopProofService(IDpopKeyService dpopKeyService, ILogger<DpopProofService> logger)
  {
    _dpopKeyService = dpopKeyService;
    _tokenHandler = new JwtSecurityTokenHandler();
    _logger = logger;
  }

  public async Task<string> CreateProofAsync(string httpMethod, string url, string accessToken = null)
  {

    _logger.LogInformation("Creating new DPoP proof for HTM: {Htm}, HTU: {Htu}", httpMethod, url);

    try
    {
      var dpopKey = await _dpopKeyService.GetOrCreateDpopKeyAsync();
      if (dpopKey == null)
      {
        _logger.LogCritical("DPoP key generation/retrieval failed (returned null). Cannot create proof.");
        throw new InvalidOperationException("Failed to get or create DPoP key.");
      }

      var jwkDictionary = JsonSerializer.Deserialize<IDictionary<string, object>>(dpopKey.PublicJwkString);
      var signingCredentials = new SigningCredentials(dpopKey.SigningKey, SecurityAlgorithms.EcdsaSha256);

      var header = new JwtHeader(signingCredentials)
      {
        [JwtHeaderParameterNames.Typ] = "dpop+jwt",
        [JwtHeaderParameterNames.Jwk] = jwkDictionary
      };

      var jti = Guid.NewGuid().ToString();
      var payload = new JwtPayload
            {
                { JwtRegisteredClaimNames.Jti, jti },
                { "htm", httpMethod.ToUpperInvariant() },
                { "htu", url },
                { JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            };

      _logger.LogDebug("[DPoP Proof Claims] JTI: {Jti}, HTM: {Htm}, HTU: {Htu}", jti, httpMethod, url);

      if (!string.IsNullOrEmpty(accessToken))
      {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
        var ath = Base64UrlEncoder.Encode(hashBytes);
        payload.Add("ath", ath);

        _logger.LogDebug("[DPoP Proof Claims] ATH: {Ath} (Access Token provided)", ath);
      }
      else
      {
        _logger.LogDebug("[DPoP Proof Claims] ATH: (Access Token not provided)");
      }

      var token = new JwtSecurityToken(header: header, payload: payload);
      var tokenString = _tokenHandler.WriteToken(token);

      _logger.LogInformation("DPoP proof created successfully. JTI: {Jti}", jti);
      return tokenString;
    }
    catch (Exception ex)
    {
      _logger.LogCritical(ex, "CRITICAL: Failed to create DPoP proof. HTM: {Htm}, HTU: {Htu}", httpMethod, url);
      throw;
    }
  }
}
