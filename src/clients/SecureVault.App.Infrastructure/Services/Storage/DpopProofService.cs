using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;

namespace SecureVault.App.Infrastructure.Services.Storage;

public class DpopProofService : IDpopProofService
{
  private readonly IDpopKeyService _dpopKeyService;
  private readonly JwtSecurityTokenHandler _tokenHandler;

  public DpopProofService(IDpopKeyService dpopKeyService)
  {
    _dpopKeyService = dpopKeyService;
    _tokenHandler = new JwtSecurityTokenHandler();
  }

  public async Task<string> CreateProofAsync(string httpMethod, string url, string accessToken = null)
  {
    var dpopKey = await _dpopKeyService.GetOrCreateDpopKeyAsync();
    var jwkDictionary = JsonSerializer.Deserialize<IDictionary<string, object>>(dpopKey.PublicJwkString);
    var signingCredentials = new SigningCredentials(dpopKey.SigningKey, SecurityAlgorithms.EcdsaSha256);

    var header = new JwtHeader(signingCredentials)
    {
      [JwtHeaderParameterNames.Typ] = "dpop+jwt",
      [JwtHeaderParameterNames.Jwk] = jwkDictionary
    };

    var payload = new JwtPayload
        {
            { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() },
            { "htm", httpMethod.ToUpperInvariant() },
            { "htu", url },
            { JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        };

    if (!string.IsNullOrEmpty(accessToken))
    {
      using var sha256 = SHA256.Create();
      var hashBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
      var ath = Base64UrlEncoder.Encode(hashBytes);
      payload.Add("ath", ath);
    }

    var token = new JwtSecurityToken(header: header, payload: payload);
    var tokenString = _tokenHandler.WriteToken(token);

    return tokenString;
  }
}
