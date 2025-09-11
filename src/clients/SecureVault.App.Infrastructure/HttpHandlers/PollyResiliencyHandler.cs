using Polly;
using Polly.Extensions.Http;
using Polly.Wrap;
using System.Net;

namespace SecureVault.App.Infrastructure.HttpHandlers
{
    public class PollyResiliencyHandler : DelegatingHandler
    {
        private static readonly AsyncPolicyWrap<HttpResponseMessage> _resiliencyPolicy = CreateResiliencyPolicy();

        private static AsyncPolicyWrap<HttpResponseMessage> CreateResiliencyPolicy()
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        Console.WriteLine($"[Polly-Retry] İstek başarısız oldu: {outcome.Result?.StatusCode}. " +
                                          $"Deneme {retryAttempt}. {timespan.TotalSeconds} saniye sonra yeniden denenecek...");
                    });

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (result, timespan, context) =>
                    {
                        Console.WriteLine($"[Polly-Breaker] Devre açıldı! Süre: {timespan.TotalSeconds} saniye.");
                    },
                    onReset: (context) =>
                    {
                        Console.WriteLine("[Polly-Breaker] Devre kapatıldı. İstekler yeniden gönderilecek.");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("[Polly-Breaker] Devre yarı-açık. Bir sonraki istek deneme amaçlı gönderilecek.");
                    }
                );

            return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _resiliencyPolicy.ExecuteAsync(() => base.SendAsync(request, cancellationToken));
        }
    }
}
