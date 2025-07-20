namespace SecureVault.Identity.Application.Contracts.DTOs.AuthDto
{
    public record LoginCredentialsDto(string Email, string Signature);
}
