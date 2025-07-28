using SecureVault.ApiGateway.MiddleWares;

namespace SecureVault.ApiGateway.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenBlacklist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenBlacklistMiddleware>();
        }
    }
}
