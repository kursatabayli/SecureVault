using System.Net.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureVault.App.Infrastructure.Helpers;

namespace SecureVault.App.Infrastructure.HttpHandlers;

internal abstract class BaseHttpHandlerPipelineBuilder
{
  protected readonly IServiceProvider ServiceProvider;
  protected readonly ILogger _logger;
  private readonly string _devHost;

  protected BaseHttpHandlerPipelineBuilder(IServiceProvider serviceProvider, ILogger logger, IOptions<ApiSettings> apiSettings)
  {
    ServiceProvider = serviceProvider;
    _logger = logger;
    var settings = apiSettings.Value;
    _devHost = string.Empty;

    if (!string.IsNullOrEmpty(settings.BaseUrl) && Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
    {
      _devHost = baseUri.Host;
    }
  }

  protected HttpClientHandler CreateFinalHandler()
  {
    return new HttpClientHandler
    {
      ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
      {
#if DEBUG
        if (!string.IsNullOrEmpty(_devHost) && message.RequestUri.Host.Equals(_devHost, StringComparison.OrdinalIgnoreCase))
        {
          _logger.LogWarning("[SSL-DEBUG] Certificate validation bypassed for {DevHost} via {HandlerType}.", _devHost, GetType().Name);
          return true;
        }
#endif
        return errors == SslPolicyErrors.None;
      }
    };
  }
}