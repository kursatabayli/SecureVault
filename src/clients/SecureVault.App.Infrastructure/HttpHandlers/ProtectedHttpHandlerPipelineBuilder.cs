using Microsoft.Extensions.Options;
using SecureVault.App.Infrastructure.Helpers;

namespace SecureVault.App.Infrastructure.HttpHandlers;

public interface IProtectedHttpHandlerPipelineBuilder
{
  DelegatingHandler CreatePipeline();
}

internal class ProtectedHttpHandlerPipelineBuilder : BaseHttpHandlerPipelineBuilder, IProtectedHttpHandlerPipelineBuilder
{
  public ProtectedHttpHandlerPipelineBuilder(IServiceProvider serviceProvider, IOptions<ApiSettings> apiSettings)
      : base(serviceProvider, apiSettings)
  {
  }

  public DelegatingHandler CreatePipeline()
  {
    var customFinalHandler = CreateFinalHandler();
    var pollyResiliencyHandler = ServiceProvider.GetRequiredService<PollyResiliencyHandler>();
    var deviceHeadersHandler = ServiceProvider.GetRequiredService<DeviceHeadersHandler>();
    var refreshTokenHandler = ServiceProvider.GetRequiredService<RefreshTokenHandler>();
    var dpopHandler = ServiceProvider.GetRequiredService<DpopHandler>();

    pollyResiliencyHandler.InnerHandler = deviceHeadersHandler;
    deviceHeadersHandler.InnerHandler = refreshTokenHandler;
    refreshTokenHandler.InnerHandler = dpopHandler;
    dpopHandler.InnerHandler = customFinalHandler;

    return pollyResiliencyHandler;
  }
}