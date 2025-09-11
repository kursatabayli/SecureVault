using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class InteractionService : IInteractionService
    {
        private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;
        private readonly ILogger<InteractionService> _logger;

        public InteractionService(ISecureVaultAnonymousApi secureVaultAnonymousApi, ILogger<InteractionService> logger)
        {
            _secureVaultAnonymousApi = secureVaultAnonymousApi;
            _logger = logger;
        }

        public async Task<Result<string>> CreateQrLoginChannelAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultAnonymousApi.CreateChannelAsync(cancellationToken);

                if (string.IsNullOrEmpty(response?.ChannelId))
                {
                    _logger.LogWarning("CreateChannelAsync başarılı bir yanıt döndü ancak ChannelId boş.");
                    return Result<string>.Failure(new Error("Api.EmptyResponse", "API'den geçerli bir kanal kimliği alınamadı."));
                }

                return Result<string>.Success(response.ChannelId);
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. QR giriş kanalı oluşturulamadı.");
                return Result<string>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "QR giriş kanalı oluşturulurken API hatası. Durum Kodu: {StatusCode}", ex.StatusCode);
                var error = await ex.GetContentAsAsync<Error>();
                return Result<string>.Failure(error ?? new Error("Api.RequestFailed", "Kanal oluşturma isteği başarısız oldu."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "QR giriş kanalı oluşturulurken ağ hatası.");
                return Result<string>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QR giriş kanalı oluşturulurken beklenmedik bir hata oluştu.");
                return Result<string>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin."));
            }
        }
    }
}
