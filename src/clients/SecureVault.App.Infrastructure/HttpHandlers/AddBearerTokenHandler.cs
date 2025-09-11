using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using System.Net.Http.Headers;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    internal class AddBearerTokenHandler : DelegatingHandler
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<AddBearerTokenHandler> _logger;
        public AddBearerTokenHandler(IStorageService storageService, ILogger<AddBearerTokenHandler> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Headers.Remove("X-Anonymous"))
                {
                    var bearer = await _storageService.GetAccessTokenAsync();

                    if (!string.IsNullOrEmpty(bearer))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                }

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddBearerTokenHandler içerisinde bir hata oluştu. İstek URI: {RequestUri}", request.RequestUri);
                throw;
            }
        }
    }
}
