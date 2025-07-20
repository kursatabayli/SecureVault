namespace SecureVault.Identity.Application.Contracts.Services
{
    public interface IEcdsaVerificationService
    {
        bool VerifySignature(string message, string signature, byte[] publicKey);
    }
}
