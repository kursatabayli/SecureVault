using SecureVault.App.Services.Service.Infrastructure.Contracts;
using System;
using System.Net;
using System.Net.Http.Headers;

namespace SecureVault.App.Services.AuthHelpers
{
    public sealed class AuthTokenHandler : DelegatingHandler
    {
        private const string RefreshTokenEndpointPath = "/identity/api/Auth/refresh/";

        private readonly SemaphoreSlim _refreshTokenLock = new(1, 1);
        private readonly IServiceProvider _serviceProvider;

        public AuthTokenHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var storageService = _serviceProvider.GetRequiredService<IStorageService>();

            if (request.Headers.TryGetValues("X-Anonymous", out _))
            {
                request.Headers.Remove("X-Anonymous");
                var bearer = await storageService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(bearer))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                return await base.SendAsync(request, cancellationToken);
            }

            var authService = _serviceProvider.GetRequiredService<IAuthService>();

            await RefreshTokenIfNeededAsync(authService, storageService, cancellationToken);

            var accessToken = await storageService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var currentToken = await storageService.GetAccessTokenAsync();
                    if (accessToken != currentToken)
                    {
                        var clonedRequestForRetry = await CloneRequestAsync(request);
                        clonedRequestForRetry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                        return await base.SendAsync(clonedRequestForRetry, cancellationToken);
                    }

                    var refreshResult = await authService.RefreshTokenAsync();
                    if (refreshResult.IsSuccess)
                    {
                        var refreshedAccessToken = await storageService.GetAccessTokenAsync();
                        var clonedRequest = await CloneRequestAsync(request);
                        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
                        return await base.SendAsync(clonedRequest, cancellationToken);
                    }
                    else
                    {
                        await authService.LogoutAsync();
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


        private async Task RefreshTokenIfNeededAsync(IAuthService authService, IStorageService storageService, CancellationToken cancellationToken)
        {
            var expirationDate = await storageService.GetAccessTokenExpirationAsync();

            if (expirationDate <= DateTime.UtcNow.AddSeconds(30))
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var newExpirationDate = await storageService.GetAccessTokenExpirationAsync();
                    if (newExpirationDate != expirationDate)
                        return;

                    await authService.RefreshTokenAsync();
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
