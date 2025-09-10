namespace SecureVault.ApiGateway.Services
{
    public interface IServiceDiscovery
    {
        Task<Uri> GetServiceAddressAsync(string serviceName);
    }
}
