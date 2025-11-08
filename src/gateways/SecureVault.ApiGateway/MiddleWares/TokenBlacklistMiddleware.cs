using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.JsonWebTokens;
using StackExchange.Redis;

namespace SecureVault.ApiGateway.MiddleWares
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenBlacklistMiddleware> _logger;
        private const string BlacklistKeyPrefix = "blacklist:";

        public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    var redisKey = $"{BlacklistKeyPrefix}{jti}";

                    var blacklistedToken = await cache.GetAsync(redisKey);

                    if (blacklistedToken != null)
                    {
                        _logger.LogWarning("Request attempt with a revoked token. JTI: {Jti}, IP: {IpAddress}", jti, context.Connection.RemoteIpAddress);

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("This token has been revoked.");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
