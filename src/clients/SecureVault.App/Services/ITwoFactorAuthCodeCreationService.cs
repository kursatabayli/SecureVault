using SecureVault.App.Models.TwoFactorAuthCodeModels;

namespace SecureVault.App.Services
{
    public interface ITwoFactorAuthCodeCreationService
    {
        Task<bool> SubmitCreationCommandAsync(TwoFactorAuthCodeModel model, string logContext);
        bool TryParseOtpAuthUri(string otpAuthUri, out TwoFactorAuthCodeModel? parsedModel);

    }
}
