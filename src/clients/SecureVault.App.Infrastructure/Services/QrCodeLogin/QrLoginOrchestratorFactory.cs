using SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

namespace SecureVault.App.Infrastructure.Services.QrCodeLogin
{
    public class QrLoginOrchestratorFactory : IQrLoginOrchestratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QrLoginOrchestratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IQrLoginOrchestrator Create()
        {
            return _serviceProvider.GetRequiredService<IQrLoginOrchestrator>();
        }
    }
}
