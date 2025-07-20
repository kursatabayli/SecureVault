namespace SecureVault.App.Services.Models.AuthModels
{
    public class ChallengeModel
    {
        public string Challenge { get; set; }
        public byte[] Salt { get; set; }
    }
}
