using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services
{
    public interface IOtpService : IAsyncDisposable
    {
        event Func<Task>? OnTick;
        bool IsLoading { get; }
        Error? InitializationError { get; }
        IReadOnlyList<OtpViewModel> Items { get; }
        Task GenerateHotpCodeAsync(Guid itemId);
        Task InitializeAsync();

        OtpViewModel? GetItem(Guid id);
    }
}
