using SecureVault.ApiGateway.MiddleWares;

namespace SecureVault.ApiGateway.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder AddMiddlewares(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>()
                          .UseMiddleware<TokenBlacklistMiddleware>();
        }
    }
}
