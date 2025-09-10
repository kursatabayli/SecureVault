namespace SecureVault.App.Application.Contracts.DTOs.Auth
{
    public class ChallengeDto
    {
        public string Challenge { get; set; }
        public byte[] Salt { get; set; }
    }
}
