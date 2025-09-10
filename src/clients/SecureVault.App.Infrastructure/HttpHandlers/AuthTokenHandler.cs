using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using System.Net;
using System.Net.Http.Headers;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    public sealed class AuthTokenHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim _refreshTokenLock = new(1, 1);
        private readonly IAuthService _authService;
        private readonly IStorageService _storageService;
        private readonly IAuthenticationStateNotifier _authenticationStateNotifier;
        private bool _disposed = false;
        public AuthTokenHandler(IAuthService authService, IStorageService storageService, IAuthenticationStateNotifier authenticationStateNotifier)
        {
            _authService = authService;
            _storageService = storageService;
            _authenticationStateNotifier = authenticationStateNotifier;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("X-Anonymous", out _))
            {
                request.Headers.Remove("X-Anonymous");
                var bearer = await _storageService.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(bearer))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                return await base.SendAsync(request, cancellationToken);
            }

            await RefreshTokenIfNeededAsync(cancellationToken);

            var accessToken = await _storageService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var currentToken = await _storageService.GetAccessTokenAsync();
                    if (accessToken != currentToken)
                    {
                        var clonedRequestForRetry = await CloneRequestAsync(request);
                        clonedRequestForRetry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentToken);
                        return await base.SendAsync(clonedRequestForRetry, cancellationToken);
                    }

                    var refreshResult = await _authService.RefreshTokenAsync(cancellationToken);
                    if (refreshResult.IsSuccess)
                    {
                        var refreshedAccessToken = await _storageService.GetAccessTokenAsync();
                        var clonedRequest = await CloneRequestAsync(request);
                        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
                        return await base.SendAsync(clonedRequest, cancellationToken);
                    }
                    else
                    {
                        await _authenticationStateNotifier.NotifyUserLogout();
                        return response;
                    }
                }
                finally
                {
                    _refreshTokenLock.Release();
                }
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                await _authenticationStateNotifier.NotifyUserLogout();
            }
            return response;
        }


        private async Task RefreshTokenIfNeededAsync(CancellationToken cancellationToken)
        {
            var expirationDate = _storageService.GetAccessTokenExpiration();

            if (expirationDate <= DateTime.UtcNow.AddSeconds(30))
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var newExpirationDate = _storageService.GetAccessTokenExpiration();
                    if (newExpirationDate != expirationDate)
                        return;

                    await _authService.RefreshTokenAsync(cancellationToken);
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

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    _refreshTokenLock.Dispose();
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
