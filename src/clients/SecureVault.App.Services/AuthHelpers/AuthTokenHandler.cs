using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Service.Contracts;
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
            var accessToken = await SecureStorage.Default.GetAsync(StorageKeys.AccessToken);
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var newAccessToken = await SecureStorage.Default.GetAsync(StorageKeys.AccessToken);
                    if (accessToken != newAccessToken)
                    {
                        response = await ResendRequestWithNewToken(request, newAccessToken, cancellationToken);
                    }
                    else
                    {
                        var refreshResult = await _authService.RefreshTokenAsync();
                        if (refreshResult.IsSuccess)
                        {
                            var refreshedAccessToken = await SecureStorage.Default.GetAsync(StorageKeys.AccessToken);
                            response = await ResendRequestWithNewToken(request, refreshedAccessToken, cancellationToken);
                        }
                        else
                        {
                            return response;
                        }
                    }
                }
                finally
                {
                    _refreshTokenLock.Release();
                }
            }

            return response;
        }

        private async Task<HttpResponseMessage> ResendRequestWithNewToken(HttpRequestMessage request, string? newToken, CancellationToken cancellationToken)
        {
            var clonedRequest = await CloneRequestAsync(request);

            if (!string.IsNullOrEmpty(newToken))
            {
                clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
            }

            return await base.SendAsync(clonedRequest, cancellationToken);
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
