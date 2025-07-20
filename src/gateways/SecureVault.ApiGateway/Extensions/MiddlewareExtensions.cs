namespace SecureVault.ApiGateway.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseIpLogging(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpLoggingMiddleware>();
        }
    }
}
