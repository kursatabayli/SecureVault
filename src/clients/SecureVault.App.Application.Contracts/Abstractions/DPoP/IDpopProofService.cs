namespace SecureVault.App.Application.Contracts.Abstractions.DPoP;

public interface IDpopProofService
{
  Task<string> CreateProofAsync(string httpMethod, string url, string accessToken = null);
}
