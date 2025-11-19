using Microsoft.AspNetCore.Components.Authorization;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using System.Security.Claims;
using System.Text.Json;

namespace SecureVault.App.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider, IAuthenticationStateNotifier
{
    private readonly IStorageService _storageService;
    public event Func<bool, Task> OnAuthenticationStateChangedAsync;

    public CustomAuthStateProvider(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = await _storageService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(accessToken))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(accessToken), "jwtAuthType"));
        return new AuthenticationState(claimsPrincipal);
    }

    public async Task NotifyUserAuthenticated(string accessToken)
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(accessToken), "jwtAuthType"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
        _storageService.SetAuthenticationState(true);
        NotifyAuthenticationStateChanged(authState);
        if (OnAuthenticationStateChangedAsync != null)
            await OnAuthenticationStateChangedAsync.Invoke(true);
    }
    public async Task NotifyUserLogout()
    {
        await _storageService.ClearAll();
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
        if (OnAuthenticationStateChangedAsync != null)
            await OnAuthenticationStateChangedAsync.Invoke(false);
    }
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];

        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs != null)
        {
            if (keyValuePairs.TryGetValue("sub", out var sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.ToString()));

            if (keyValuePairs.TryGetValue("email", out var email))
                claims.Add(new Claim(ClaimTypes.Email, email.ToString()));
        }

        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}