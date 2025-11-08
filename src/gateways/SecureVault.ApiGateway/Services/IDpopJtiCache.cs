namespace SecureVault.ApiGateway.Services;

public interface IDpopJtiCache
{
  Task<bool> IsJtiReplayedAsync(string jti);

  Task StoreJtiAsync(string jti, DateTimeOffset expiration);
}