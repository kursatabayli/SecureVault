using SecureVault.App.Services.Models.VaultItemModels;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface ITotpService : IDisposable
    {
        event Action? OnTick;

        IReadOnlyList<TotpViewModel> Items { get; }

        Task InitializeAsync();

        TotpViewModel? GetItem(Guid id);
    }
}
