namespace SecureVault.App.Services.Service.Infrastructure.Implementations
{
    public class LogoutService
    {
        public event Action? OnLogoutRequested;

        public void RequestLogout() => OnLogoutRequested?.Invoke();
    }
}
