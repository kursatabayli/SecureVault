namespace SecureVault.Interaction.Api.Features.Sync.Contracts
{
    public interface INotificationService
    {
        Task NotifyClientsForSync(Guid userId, string originDeviceId);
    }
}
