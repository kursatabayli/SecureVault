using MediatR;
using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.Shared.Result;

namespace SecureVault.App.Application.Features.CQRS.Auth.Handlers
{
    public class LogoutHandler : IRequestHandler<LogoutCommand, Result>
    {
        private readonly IAuthService _authService;
        private readonly IStorageService _storageService;
        private readonly ILogger<LogoutHandler> _logger;

        public LogoutHandler(IAuthService authService, IStorageService storageService, ILogger<LogoutHandler> logger)
        {
            _authService = authService;
            _storageService = storageService;
            _logger = logger;
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
                await _storageService.ClearAll();
                _logger.LogWarning("Yerel veritabanı siliniyor...");
                _logger.LogInformation("Yerel veritabanı başarıyla silindi.");
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
