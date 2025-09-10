namespace SecureVault.App.Application.Contracts.Abstractions.UI
{
    public interface IAuthenticationStateNotifier
    {
        event Func<bool, Task> OnAuthenticationStateChangedAsync;
        Task NotifyUserAuthenticated(string accessToken);
        Task NotifyUserLogout();
    }
}
