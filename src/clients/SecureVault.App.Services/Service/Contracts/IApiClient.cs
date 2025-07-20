using SecureVault.App.Services.Constants;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Contracts
{
    public interface IApiClient
    {
        Task<Result<TResponse>> GetAsync<TResponse>(string endpoint, ClientTypes clientType = ClientTypes.PublicClient);
        Task<Result> PostAsync<TRequest>(string endpoint, TRequest payload, ClientTypes clientType = ClientTypes.PublicClient);
        Task<Result<TResponse>> PostAsync<TRequest, TResponse>(
            string endpoint, 
            TRequest payload, 
            ClientTypes clientType = ClientTypes.PublicClient,
            Func<HttpRequestMessage, Task>? configureRequestAsync = null);
    }
}
