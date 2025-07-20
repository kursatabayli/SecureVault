using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface ITotpService : IDisposable
    {
        event Action? OnTick;
        bool IsLoading { get; }
        Error? InitializationError { get; }
        IReadOnlyList<TotpViewModel> Items { get; }

        Task InitializeAsync();

        TotpViewModel? GetItem(Guid id);
    }
}
