using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace SecureVault.ApiGateway.MiddleWares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetCorrelationId(context);

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                AddCorrelationIdToResponse(context, correlationId);
                await _next(context);
            }
        }

        private static StringValues GetCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) && !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            var newCorrelationId = Guid.NewGuid().ToString();
            context.Request.Headers.Add(CorrelationIdHeaderName, newCorrelationId);
            return newCorrelationId;
        }

        private static void AddCorrelationIdToResponse(HttpContext context, StringValues correlationId)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers.Add(CorrelationIdHeaderName, correlationId);
                }
                return Task.CompletedTask;
            });
        }
    }
}
