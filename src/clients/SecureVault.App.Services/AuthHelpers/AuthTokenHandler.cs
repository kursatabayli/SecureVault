using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Service.Contracts;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace SecureVault.App.Services.AuthHelpers
{
    public sealed class AuthTokenHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim _refreshTokenLock = new(1, 1);
        private readonly IAuthService _authService;
        public AuthTokenHandler(IAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await RefreshTokenIfNeededAsync(cancellationToken);

            var accessToken = await SecureStorage.Default.GetAsync(StorageItems.AccessToken);
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var currentToken = await SecureStorage.Default.GetAsync(StorageItems.AccessToken);
                    if (accessToken != currentToken)
                    {
                        var clonedRequestForRetry = await CloneRequestAsync(request);
                        clonedRequestForRetry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                        return await base.SendAsync(clonedRequestForRetry, cancellationToken);
                    }

                    var refreshResult = await _authService.RefreshTokenAsync();
                    if (refreshResult.IsSuccess)
                    {
                        var refreshedAccessToken = await SecureStorage.Default.GetAsync(StorageItems.AccessToken);
                        var clonedRequest = await CloneRequestAsync(request);
                        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
                        return await base.SendAsync(clonedRequest, cancellationToken);
                    }
                    else
                    {
                        await _authService.LogoutAsync();
                        return response;
                    }
                }
                finally
                {
                    _refreshTokenLock.Release();
                }
            }

            return response;
        }


        private async Task RefreshTokenIfNeededAsync(CancellationToken cancellationToken)
        {
            var expirationString = await SecureStorage.Default.GetAsync(StorageItems.AccessTokenExpiration);
            if (string.IsNullOrEmpty(expirationString) || !DateTime.TryParse(expirationString, out var expirationDate))
                return;

            if (expirationDate.ToUniversalTime() <= DateTime.UtcNow.AddSeconds(30))
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var newExpirationString = await SecureStorage.Default.GetAsync(StorageItems.AccessTokenExpiration);
                    if (newExpirationString != expirationString)
                        return;

                    await _authService.RefreshTokenAsync();
                }
                finally
                {
                    _refreshTokenLock.Release();
                }
            }
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage originalRequest)
        {
            var clone = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri)
            {
                Version = originalRequest.Version
            };

            if (originalRequest.Content != null)
            {
                var ms = new MemoryStream();
                await originalRequest.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                foreach (var header in originalRequest.Content.Headers)
                {
                    clone.Content.Headers.Add(header.Key, header.Value);
                }
            }

            foreach (var header in originalRequest.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in originalRequest.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value);
            }

            return clone;
        }
    }
}
