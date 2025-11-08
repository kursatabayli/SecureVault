using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using System.Net;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    public sealed class RefreshTokenHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim _refreshTokenLock = new(1, 1);
        private readonly IAuthService _authService;
        private readonly IStorageService _storageService;
        private readonly IAuthenticationStateNotifier _authenticationStateNotifier;
        private readonly ILogger<RefreshTokenHandler> _logger;
        private bool _disposed = false;
        public RefreshTokenHandler(IAuthService authService, IStorageService storageService, IAuthenticationStateNotifier authenticationStateNotifier, ILogger<RefreshTokenHandler> logger)
        {
            _authService = authService;
            _storageService = storageService;
            _authenticationStateNotifier = authenticationStateNotifier;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            await RefreshTokenIfNeededAsync(cancellationToken);

            var tokenBeforeSend = await _storageService.GetAccessTokenAsync();
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _refreshTokenLock.WaitAsync(cancellationToken);
                try
                {
                    var tokenAfterWait = await _storageService.GetAccessTokenAsync();
                    if (tokenBeforeSend != tokenAfterWait)
                    {
                        var clonedRequestForRetry = await CloneRequestAsync(request);
                        return await base.SendAsync(clonedRequestForRetry, cancellationToken);
                    }

                    _logger.LogWarning("RefreshTokenHandler: Received 401, initiating token refresh.");
                    var refreshResult = await _authService.RefreshTokenAsync(cancellationToken);

                    if (refreshResult.IsSuccess)
                    {
                        _logger.LogInformation("RefreshTokenHandler: Token successfully refreshed.");
                        var clonedRequest = await CloneRequestAsync(request);

                        return await base.SendAsync(clonedRequest, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError("RefreshTokenHandler: Refresh token renewal FAILED. Terminating session.");
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
                _logger.LogWarning("RefreshTokenHandler: Received 403 Forbidden. Terminating session.");
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
