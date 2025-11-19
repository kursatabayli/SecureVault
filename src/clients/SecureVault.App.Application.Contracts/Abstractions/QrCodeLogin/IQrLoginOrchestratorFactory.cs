namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin;

public interface IQrLoginOrchestratorFactory
{
    IQrLoginOrchestrator Create();
}
