using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using System.Net.Http.Headers;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    internal class AddBearerTokenHandler : DelegatingHandler
    {
        private readonly IStorageService _storageService;

        public AddBearerTokenHandler(IStorageService storageService)
        {
            _storageService = storageService;
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
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
