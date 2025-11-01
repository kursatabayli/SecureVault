using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Database;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Contracts.Abstractions.UI;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers
{
    public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IStorageService _storageService;
        private readonly ILogger<LogoutHandler> _logger;
        private readonly IDatabaseManager _databaseManager;
        private readonly IAuthenticationStateNotifier _authenticationStateNotifier;

        public LogoutHandler(IAuthService authService, IStorageService storageService, ILogger<LogoutHandler> logger, IDatabaseManager databaseManager, IAuthenticationStateNotifier authenticationStateNotifier)
        {
            _authService = authService;
            _storageService = storageService;
            _logger = logger;
            _databaseManager = databaseManager;
            _authenticationStateNotifier = authenticationStateNotifier;
        }

        public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.LogoutAsync(cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError("Kullanıcı çıkışı yapılırken bir hata oluştu: {Error}", result.Error);
            }

            try
            {
                _logger.LogInformation("Kalan depolama verileri (örn: tokenlar) siliniyor...");
                await _storageService.ClearAll();
                _logger.LogInformation("Yerel veritabanı ve tüm veriler başarıyla silindi.");
                await _databaseManager.CleanupOldDbFilesAsync();
                await _authenticationStateNotifier.NotifyUserLogout();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Yerel veritabanı silinirken kritik bir hata oluştu.");
                return Result.Failure(new Error("Database.Clear.Failed", "Yerel veriler temizlenemedi."));
            }
            return Result.Success();
        }
    }
}
