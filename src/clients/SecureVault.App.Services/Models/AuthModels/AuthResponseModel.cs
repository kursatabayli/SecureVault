namespace SecureVault.App.Services.Models.AuthModels
{
    public record AuthResponseModel(string AccessToken, string? RefreshToken, DateTime AccessTokenExpiration, DateTime? RefreshTokenExpiration);
}
