namespace SecureVault.Identity.Application.Contracts.DTOs.AuthDto
{
    public record RefreshTokenRequestDto(string RefreshToken, string AccessToken);
}
