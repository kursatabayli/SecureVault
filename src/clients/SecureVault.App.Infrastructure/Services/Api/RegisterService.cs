using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Register;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class RegisterService : IRegisterService
    {
        private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;
        private readonly ILogger<RegisterService> _logger;

        public RegisterService(ISecureVaultAnonymousApi secureVaultAnonymousApi, ILogger<RegisterService> logger)
        {
            _secureVaultAnonymousApi = secureVaultAnonymousApi;
            _logger = logger;
        }

        public async Task<Result?> RegisterAsync(RegisterUserDto registerUserDto, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultAnonymousApi.RegisterAsync(registerUserDto, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return Result.Success();

                var error = await response.Error.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.RegisterFailed", "Kayıt başarısız."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kayıt işlemi yapılamadı.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kayıt işlemi sırasında API hatası. Durum Kodu: {StatusCode}", ex.StatusCode);
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Api.RequestFailed", "Kayıt olma isteği başarısız oldu."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kayıt işlemi sırasında ağ hatası.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kayıt işlemi sırasında beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin."));
            }
        }
    }
}
