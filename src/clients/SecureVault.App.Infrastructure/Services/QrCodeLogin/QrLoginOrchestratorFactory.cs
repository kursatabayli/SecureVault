using Microsoft.Extensions.Logging;
using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin;

public class QrLoginOrchestratorFactory : IQrLoginOrchestratorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QrLoginOrchestratorFactory> _logger;

    public QrLoginOrchestratorFactory(IServiceProvider serviceProvider, ILogger<QrLoginOrchestratorFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public IQrLoginOrchestrator Create()
    {
        _logger.LogInformation("Creating new 'IQrLoginOrchestrator' instance from factory...");
        return _serviceProvider.GetRequiredService<IQrLoginOrchestrator>();
    }
}
