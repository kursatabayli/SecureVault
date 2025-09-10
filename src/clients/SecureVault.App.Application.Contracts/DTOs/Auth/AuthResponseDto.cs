namespace SecureVault.App.Application.Contracts.DTOs.Auth
{
    public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime AccessTokenExpiration, DateTime RefreshTokenExpiration);
}
