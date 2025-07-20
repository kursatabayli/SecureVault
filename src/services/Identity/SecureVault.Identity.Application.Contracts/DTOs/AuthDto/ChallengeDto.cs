namespace SecureVault.Identity.Application.Contracts.DTOs.AuthDto
{
    public class ChallengeDto
    {
        public string Challenge { get; set; }
        public byte[] Salt { get; set; }
    }
}
