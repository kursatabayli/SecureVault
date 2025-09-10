namespace SecureVault.ApiGateway.Handlers
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CorrelationIdHeaderName = "X-Correlation-ID";

        public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId))
            {
                request.Headers.Add(CorrelationIdHeaderName, correlationId.ToString());
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
