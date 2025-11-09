using Microsoft.Extensions.Options;
using SecureVault.App.Infrastructure.Helpers;

namespace SecureVault.App.Infrastructure.HttpHandlers;

public interface IPublicHttpHandlerPipelineBuilder
{
  DelegatingHandler CreatePipeline();
}

internal class PublicHttpHandlerPipelineBuilder : BaseHttpHandlerPipelineBuilder, IPublicHttpHandlerPipelineBuilder
{
  public PublicHttpHandlerPipelineBuilder(IServiceProvider serviceProvider, IOptions<ApiSettings> apiSettings)
      : base(serviceProvider, apiSettings)
  {
  }

  public DelegatingHandler CreatePipeline()
  {
    var customFinalHandler = CreateFinalHandler();

    var pollyResiliencyHandler = ServiceProvider.GetRequiredService<PollyResiliencyHandler>();
    var deviceHeadersHandler = ServiceProvider.GetRequiredService<DeviceHeadersHandler>();
    var dpopHandler = ServiceProvider.GetRequiredService<DpopHandler>();

    pollyResiliencyHandler.InnerHandler = deviceHeadersHandler;
    deviceHeadersHandler.InnerHandler = dpopHandler;
    dpopHandler.InnerHandler = customFinalHandler;

    return pollyResiliencyHandler;
  }
}