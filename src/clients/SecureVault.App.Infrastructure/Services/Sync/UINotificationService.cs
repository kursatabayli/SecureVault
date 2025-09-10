namespace SecureVault.App.Application.Services
{
    public static class UINotificationService
    {
        public static event Func<Task>? OnVaultDataChangedAsync;

        public static async Task NotifyVaultDataChanged()
        {
            if (OnVaultDataChangedAsync != null)
                await OnVaultDataChangedAsync.Invoke();
        }
    }
}
