using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using SecureVault.ApiGateway.Services;
using Serilog;

namespace SecureVault.ApiGateway.Helpers;

public static class DpopValidationHelpers
{
  private static readonly JwtSecurityTokenHandler _tokenHandler = new();
  public static async Task<(bool IsValid, string? Jkt)> ValidateDpopProof(string dpopProofJwt, string httpMethod, string httpUrl, string? accessToken, IDpopJtiCache jtiCache)
  {
    Log.Debug("[DPoP] Validation Started. Server Method={Htm}, Server URL={Htu}", httpMethod, httpUrl);
    try
    {
      var unvalidatedToken = _tokenHandler.ReadJwtToken(dpopProofJwt);
      if (unvalidatedToken.Header.Typ != "dpop+jwt")
      {
        Log.Warning("[DPoP] Error: Token 'typ' header is not 'dpop+jwt'. Received: {Typ}", unvalidatedToken.Header.Typ);
        return (false, null);
      }

      if (!unvalidatedToken.Header.TryGetValue("jwk", out object? jwkObject))
      {
        Log.Warning("[DPoP] Error: Token 'jwk' header not found.");
        return (false, null);
      }

      var jwkJson = JsonSerializer.Serialize(jwkObject);
      var jwk = new JsonWebKey(jwkJson);

      var jti = unvalidatedToken.Payload.Jti;
      if (string.IsNullOrEmpty(jti))
      {
        Log.Warning("[DPoP] Error: 'jti' (JWT ID) is missing in DPoP proof.");
        return (false, null);
      }

      if (await jtiCache.IsJtiReplayedAsync(jti))
      {
        Log.Warning("[DPoP] Error: DPoP JTI REPLAY DETECTED! JTI: {Jti}", jti);
        return (false, null);
      }

      var validationParameters = new TokenValidationParameters
      {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        IssuerSigningKey = jwk,
        ValidTypes = ["dpop+jwt"]
      };

      _tokenHandler.ValidateToken(dpopProofJwt, validationParameters, out var validatedToken);
      var validatedProof = validatedToken as JwtSecurityToken;

      var iat = validatedProof.Payload.Iat;
      var iatTime = DateTimeOffset.FromUnixTimeSeconds(iat.Value);

      if (!iat.HasValue || iatTime < DateTimeOffset.UtcNow.AddMinutes(-5) || iatTime > DateTimeOffset.UtcNow.AddMinutes(5))
      {
        Log.Warning("[DPoP] Error: 'iat' (timestamp) is outside the valid time range.");
        return (false, null);
      }

      var htm = validatedProof.Payload.Claims.FirstOrDefault(c => c.Type == "htm")?.Value;
      var htu = validatedProof.Payload.Claims.FirstOrDefault(c => c.Type == "htu")?.Value;

      Log.Debug("[DPoP] Token Values: Token Method={TokenHtm}, Token URL={TokenHtu}", htm, htu);

      if (!string.Equals(htm, httpMethod, StringComparison.OrdinalIgnoreCase))
      {
        Log.Warning("[DPoP] Error: 'htm' (Method) mismatch. Server: {ServerMethod}, Token: {TokenMethod}", httpMethod, htm);
        return (false, null);
      }

      if (!string.Equals(htu, httpUrl, StringComparison.Ordinal))
      {
        Log.Warning("[DPoP] Error: 'htu' (URL) mismatch. Server: {ServerUrl}, Token: {TokenUrl}", httpUrl, htu);
        return (false, null);
      }
      var ath = validatedProof.Payload.Claims.FirstOrDefault(c => c.Type == "ath")?.Value;

      if (!string.IsNullOrEmpty(accessToken))
      {
        if (string.IsNullOrEmpty(ath))
        {
          Log.Warning("[DPoP] Error: Access Token provided but 'ath' claim is missing in DPoP proof.");
          return (false, null);
        }

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.ASCII.GetBytes(accessToken));
        var expectedAth = Base64UrlEncoder.Encode(hashBytes);

        if (!string.Equals(expectedAth, ath, StringComparison.Ordinal))
        {
          Log.Warning("[DPoP] Error: 'ath' (access token hash) mismatch.");
          return (false, null);
        }
        Log.Debug("[DPoP] 'ath' validation successful.");
      }
      else
      {
        if (!string.IsNullOrEmpty(ath))
        {
          Log.Warning("[DPoP] Error: Access Token not provided but 'ath' claim found in DPoP proof.");
          return (false, null);
        }
      }

      var jktBytes = jwk.ComputeJwkThumbprint();
      var jkt = Base64UrlEncoder.Encode(jktBytes);

      Log.Debug("[DPoP] Validation successful. JKT: {Jkt}", jkt);

      var expiration = iatTime.AddMinutes(5);
      await jtiCache.StoreJtiAsync(jti, expiration);

      return (true, jkt);
    }
    catch (Exception ex)
    {
      Log.Error(ex, "[DPoP] Unexpected exception occurred during validation.");
      return (false, null);
    }
  }

  public static string? GetJktFromCnf(string? cnfJson)
  {
    if (string.IsNullOrEmpty(cnfJson))
      return null;

    try
    {
      using var jsonDoc = JsonDocument.Parse(cnfJson);
      if (jsonDoc.RootElement.TryGetProperty("jkt", out var jktElement))
        return jktElement.GetString();

      return null;
    }

    catch (JsonException)
    {
      return null;
    }
  }
  public static string GetRequestUriWithoutQuery(HttpRequest request)
  {
    return new Uri($"{request.Scheme}://{request.Host}{request.Path}").AbsoluteUri;
  }

}
