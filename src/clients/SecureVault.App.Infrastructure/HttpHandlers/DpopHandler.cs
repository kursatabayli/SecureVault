using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using System.Net.Http.Headers;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    internal class DpopHandler : DelegatingHandler
    {
        private readonly IStorageService _storageService;
        private readonly IDpopProofService _dpopProofService;
        private readonly ILogger<DpopHandler> _logger;

        public DpopHandler(
            IStorageService storageService,
            IDpopProofService dpopProofService,
            ILogger<DpopHandler> logger)
        {
            _storageService = storageService;
            _dpopProofService = dpopProofService;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var accessToken = await _storageService.GetAccessTokenAsync();
                string htu = request.RequestUri.GetLeftPart(UriPartial.Path);
                var dpopProof = await _dpopProofService.CreateProofAsync(request.Method.Method, htu, accessToken);
                request.Headers.Add("DPoP", dpopProof);

                if (!string.IsNullOrEmpty(accessToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("DPoP", accessToken);
                else
                    _logger.LogWarning("DpopHandler: Authentication was requested for {RequestUri}, but no Access Token was found in storage.", request.RequestUri);

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred within DpopHandler. Request URI: {RequestUri}", request.RequestUri);
                throw;
            }
        }
    }
}
