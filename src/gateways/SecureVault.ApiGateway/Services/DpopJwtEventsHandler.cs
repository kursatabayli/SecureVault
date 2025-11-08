using Microsoft.AspNetCore.Authentication.JwtBearer;
using SecureVault.ApiGateway.Helpers;

namespace SecureVault.ApiGateway.Services;

public class DpopJwtEventsHandler
{
  private readonly IDpopJtiCache _jtiCache;
  private readonly ILogger<DpopJwtEventsHandler> _logger;

  public DpopJwtEventsHandler(IDpopJtiCache jtiCache, ILogger<DpopJwtEventsHandler> logger)
  {
    _jtiCache = jtiCache;
    _logger = logger;
  }

  public async Task HandleMessageReceived(MessageReceivedContext context)
  {
    string? dpopProof = context.Request.Headers["DPoP"];
    string? authorizationHeader = context.Request.Headers.Authorization;
    var requestUri = DpopValidationHelpers.GetRequestUriWithoutQuery(context.Request);

    if (string.IsNullOrEmpty(dpopProof))
    {
      _logger.LogWarning("Missing DPoP proof header. Request denied. URI: {Uri}", requestUri);
      context.Fail("Missing DPoP proof header.");
      return;
    }

    string? accessToken = null;
    if (!string.IsNullOrEmpty(authorizationHeader))
    {
      if (authorizationHeader.StartsWith("DPoP ", StringComparison.OrdinalIgnoreCase))
      {
        accessToken = authorizationHeader.Substring("DPoP ".Length).Trim();
        context.Token = accessToken;
      }
      else
      {
        _logger.LogWarning("Unsupported Authorization scheme. Only 'DPoP' is allowed. URI: {Uri}", requestUri);
        context.Fail("Unsupported authorization scheme. Use DPoP.");
        return;
      }
    }

    try
    {
      var (isValidProof, jkt) = await DpopValidationHelpers.ValidateDpopProof(
          dpopProof,
          context.Request.Method,
          requestUri,
          accessToken,
          _jtiCache
      );

      if (!isValidProof || jkt is null)
      {
        _logger.LogWarning("Invalid DPoP proof. Request denied. URI: {Uri}", requestUri);
        context.Fail("Invalid DPoP proof.");
        return;
      }

      context.HttpContext.Items["ValidatedDPoPThumbprint"] = jkt;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "DPoP proof validation error.");
      context.Fail("DPoP proof validation failed.");
      return;
    }

    if (!string.IsNullOrEmpty(authorizationHeader))
    {
      if (authorizationHeader.StartsWith("DPoP ", StringComparison.OrdinalIgnoreCase))
      {
        context.Token = authorizationHeader.Substring("DPoP ".Length).Trim();
      }
      else
      {
        _logger.LogWarning("Unsupported Authorization scheme. Only 'DPoP' is allowed. URI: {Uri}", requestUri);
        context.Fail("Unsupported authorization scheme. Use DPoP.");
        return;
      }
    }
  }

  public Task HandleTokenValidated(TokenValidatedContext context)
  {
    var proofJkt = context.HttpContext.Items["ValidatedDPoPThumbprint"] as string;

    var cnfClaim = context.Principal?.Claims.FirstOrDefault(c => c.Type == "cnf");
    var tokenJkt = DpopValidationHelpers.GetJktFromCnf(cnfClaim?.Value);

    _logger.LogInformation("[DPoP] proofJkt (from Proof Header): {ProofJkt}", proofJkt);
    _logger.LogInformation("[DPoP] tokenJkt (from Access Token cnf): {TokenJkt}", tokenJkt);

    if (string.IsNullOrEmpty(proofJkt) && string.IsNullOrEmpty(tokenJkt))
    {
      return Task.CompletedTask;
    }

    if (!string.IsNullOrEmpty(proofJkt) && !string.IsNullOrEmpty(tokenJkt))
    {
      if (string.Equals(proofJkt, tokenJkt, StringComparison.Ordinal))
      {
        _logger.LogInformation("[DPoP] JKT'ler başarıyla eşleşti.");
        return Task.CompletedTask;
      }

      _logger.LogWarning("[DPoP] JKT'ler EŞLEŞMİYOR! Proof={ProofJkt}, Token={TokenJkt}", proofJkt, tokenJkt);
      context.HttpContext.Items.Remove("ValidatedDPoPThumbprint");
      context.Fail("DPoP token-binding mismatch.");
      return Task.CompletedTask;
    }

    context.HttpContext.Items.Remove("ValidatedDPoPThumbprint");

    if (string.IsNullOrEmpty(proofJkt))
      context.Fail("DPoP token requires DPoP proof.");
    else
      context.Fail("DPoP proof provided for a non-DPoP token.");

    return Task.CompletedTask;
  }
}
