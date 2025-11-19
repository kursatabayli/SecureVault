using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.DPoP;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using System.Net.Http.Headers;

namespace SecureVault.App.Infrastructure.HttpHandlers;

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
            _logger.LogDebug("DpopHandler: Intercepting request to {RequestUri}. Generating DPoP proof...", request.RequestUri);

            var accessToken = await _storageService.GetAccessTokenAsync();
            string htu = request.RequestUri!.GetLeftPart(UriPartial.Path);

            var dpopProof = await _dpopProofService.CreateProofAsync(request.Method.Method, htu, accessToken);

            request.Headers.Add("DPoP", dpopProof);

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("DPoP", accessToken);
                _logger.LogDebug("DpopHandler: Successfully attached DPoP proof and 'Authorization: DPoP' header.");
            }
            else
            {
                _logger.LogDebug("DpopHandler: Attached DPoP proof (no Access Token found). Proceeding with unauthenticated DPoP request.");
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "DpopHandler: The request to {RequestUri} was canceled.", request.RequestUri);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "DpopHandler: CRITICAL error during DPoP proof creation or attachment. Request to {RequestUri} will fail.", request.RequestUri);
            throw;
        }
    }
}
