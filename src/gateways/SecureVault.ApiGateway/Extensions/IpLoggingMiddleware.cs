namespace SecureVault.ApiGateway.Extensions
{
    public class IpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpLoggingMiddleware> _logger;

        public IpLoggingMiddleware(RequestDelegate next, ILogger<IpLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation("--> Ocelot'a gelen istek IP: {ClientIP}", ipAddress);

            Console.WriteLine($"--> Ocelot'a gelen istek IP: {ipAddress}");

            await _next(context);
        }
    }
}
