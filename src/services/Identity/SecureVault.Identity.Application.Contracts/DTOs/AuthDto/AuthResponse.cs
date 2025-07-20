namespace SecureVault.Identity.Application.Contracts.DTOs.AuthDto
{
    public class AuthResponse
    {
        public string AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public DateTime AccessTokenExpiration { get; init; }
        public DateTime? RefreshTokenExpiration { get; init; }
    }
}
