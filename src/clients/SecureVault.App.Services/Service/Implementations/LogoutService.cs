namespace SecureVault.App.Services.Service.Implementations
{
    public class LogoutService
    {
        public event Action? OnLogoutRequested;

        public void RequestLogout() => OnLogoutRequested?.Invoke();
    }
}
