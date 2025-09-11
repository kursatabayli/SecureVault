using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Session;
using SecureVault.Shared.Result;
using System.Threading;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ISecureVaultAuthorizeApi _secureVaultApi;
        private readonly ILogger<UserSessionService> _logger;

        public UserSessionService(ISecureVaultAuthorizeApi secureVaultApi, ILogger<UserSessionService> logger)
        {
            _secureVaultApi = secureVaultApi;
            _logger = logger;
        }

        public async Task<Result<List<UserSessionsDto>>> GetUserSessionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var sessions = await _secureVaultApi.GetUserSessionsAsync(cancellationToken);
                return sessions;
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Kullanıcı oturumları alınamadı.");
                return Result<List<UserSessionsDto>>.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Kullanıcı oturumları alınırken API hatası. Durum Kodu: {StatusCode}", ex.StatusCode);
                var error = await ex.GetContentAsAsync<Error>();
                return Result<List<UserSessionsDto>>.Failure(error ?? new Error("Api.RequestFailed", "Oturumlar yüklenemedi."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Kullanıcı oturumları alınırken ağ hatası.");
                return Result<List<UserSessionsDto>>.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı oturumları alınırken beklenmedik bir hata oluştu.");
                return Result<List<UserSessionsDto>>.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }

        public async Task<Result> LogoutAnyWhereAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultApi.LogoutSessionAsync(sessionId, cancellationToken);

                return response.IsSuccessStatusCode
                    ? Result.Success()
                    : Result.Failure(await response.Error.GetContentAsAsync<Error>() ?? new Error("Client.DeleteFailed", "Oturum sonlandırılamadı."));
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError("Devre açık. Oturum sonlandırılamadı.");
                return Result.Failure(new Error("Service.Unavailable", "Servis geçici olarak kullanılamıyor. Lütfen birkaç dakika sonra tekrar deneyin."));
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Oturum sonlandırılırken API hatası. Durum Kodu: {StatusCode}", ex.StatusCode);
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.DeleteFailed", "Oturum sonlandırılamadı."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Oturum sonlandırılırken ağ hatası.");
                return Result.Failure(new Error("Service.ConnectionError", "Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol edin."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oturum sonlandırılırken beklenmedik bir hata oluştu.");
                return Result.Failure(new Error("Client.UnexpectedError", "Beklenmedik bir hata oluştu."));
            }
        }
    }
}
