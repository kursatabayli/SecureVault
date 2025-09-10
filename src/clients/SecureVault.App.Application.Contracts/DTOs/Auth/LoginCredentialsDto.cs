namespace SecureVault.App.Application.Contracts.DTOs.Auth
{
    public record LoginCredentialsDto(string Email, string Signature);
}
