namespace SecureVault.App.Infrastructure.HttpHandlers
{
    public interface IHttpHandlerPipelineBuilder
    {
        DelegatingHandler CreatePipeline();
    }
}
