using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.Sync;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Application.Contracts.DTOs.Auth;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers
{
    internal class LoginHandler : IRequestHandler<LoginCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;
        private readonly ILocalVaultService _localVaultService;
        private readonly ISyncLock _syncLock;
        private readonly IAuthenticationStateNotifier _authenticationStateNotifier;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(IAuthService authService, IMapper mapper, ILocalVaultService localVaultService, ISyncLock syncLock, IAuthenticationStateNotifier authenticationStateNotifier, ILogger<LoginHandler> logger)
        {
            _authService = authService;
            _mapper = mapper;
            _localVaultService = localVaultService;
            _syncLock = syncLock;
            _authenticationStateNotifier = authenticationStateNotifier;
            _logger = logger;
        }

        public async Task<Result> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var loginDto = _mapper.Map<LoginDto>(request);

            var loginResult = await _authService.LoginAsync(loginDto, cancellationToken);

            if (loginResult.IsFailure)
                return loginResult;

            _logger.LogInformation("Login başarılı, ilk senkronizasyon için kilit alınıyor...");
            if (!await _syncLock.WaitAsync(TimeSpan.FromMinutes(1), cancellationToken))
            {
                _logger.LogError("Login işlemi sırasında senkronizasyon kilidi alınamadı (timeout).");
                return Result.Failure(new Error("Client.Login.LockFailed", "Sistem meşgul, lütfen tekrar deneyin."));
            }

            _logger.LogInformation("Kilit alındı.");

            try
            {

                _logger.LogInformation("AuthState bildiriliyor...");
                await _authenticationStateNotifier.NotifyUserAuthenticated(loginResult.Value.AccessToken);

                _logger.LogInformation("İlk kasa verisi yüklemesi başlatılıyor...");
                var setVaultResult = await _localVaultService.SetAllVaultDataAsync(cancellationToken);

                if (setVaultResult.IsFailure)
                {
                    _logger.LogError("İlk kasa verisi yüklemesi başarısız: {Error}", setVaultResult.Error);
                    return Result.Failure(setVaultResult.Error);
                }

                _logger.LogInformation("İlk kasa verisi yüklemesi tamamlandı.");

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login-Sync orkestrasyonunda beklenmedik hata.");
                return Result.Failure(new Error("Client.Login.Critical", "Kritik bir hata oluştu."));
            }
            finally
            {
                _logger.LogInformation("Login kilidi bırakılıyor.");
                _syncLock.Release();
            }
        }
    }
}
