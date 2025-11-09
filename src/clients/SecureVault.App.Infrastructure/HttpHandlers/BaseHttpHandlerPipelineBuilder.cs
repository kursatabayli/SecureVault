using System.Net.Security;
using Microsoft.Extensions.Options;
using SecureVault.App.Infrastructure.Helpers;

namespace SecureVault.App.Infrastructure.HttpHandlers;

internal abstract class BaseHttpHandlerPipelineBuilder
{
  protected readonly IServiceProvider ServiceProvider;
  private readonly string _devHost;

  protected BaseHttpHandlerPipelineBuilder(IServiceProvider serviceProvider, IOptions<ApiSettings> apiSettings)
  {
    ServiceProvider = serviceProvider;
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
          Console.WriteLine($"[SSL-DEBUG] Certificate validation bypassed for {_devHost} via {GetType().Name}.");
          return true;
        }
#endif
        return errors == SslPolicyErrors.None;
      }
    };
  }
}