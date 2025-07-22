using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services
{
    public interface IOtpService : IDisposable
    {
        event Action? OnTick;
        bool IsLoading { get; }
        Error? InitializationError { get; }
        IReadOnlyList<OtpViewModel> Items { get; }
        Task GenerateHotpCodeAsync(Guid itemId);
        Task InitializeAsync();

        OtpViewModel? GetItem(Guid id);
    }
}
