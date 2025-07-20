using Microsoft.Extensions.Localization;
using SecureVault.App.Services.Constants;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Contracts;
using SecureVault.Shared.Result;
using System.Diagnostics;
using System.Net.Http.Json;

namespace SecureVault.App.Services.Service.Implementations
{
    public class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ApiClient(IHttpClientFactory httpClientFactory, IStringLocalizer<SharedResources> localizer)
        {
            _httpClientFactory = httpClientFactory;
            _localizer = localizer;
        }

        public async Task<Result<TResponse>> GetAsync<TResponse>(string endpoint, ClientTypes clientType)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(clientType.ToString());
                var response = await client.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                else
                    return await response.Content.ReadFromJsonAsync<Error>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API Request Failed: {ex.Message}");
                return new Error(ErrorCodes.Client.NetworkError, _localizer[ErrorCodes.Client.NetworkError]);
            }
        }

        public async Task<Result> PostAsync<TRequest>(string endpoint, TRequest payload, ClientTypes clientType)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(clientType.ToString());
                var response = await client.PostAsJsonAsync(endpoint, payload);

                if (response.IsSuccessStatusCode)
                    return Result.Success();
                else
                    return Result.Failure(await response.Content.ReadFromJsonAsync<Error>());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API Request Failed: {ex.Message}");
                return Result.Failure(new Error(ErrorCodes.Client.NetworkError, _localizer[ErrorCodes.Client.NetworkError]));
            }
        }

        public async Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, ClientTypes clientType, Func<HttpRequestMessage, Task>? configureRequestAsync = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(clientType.ToString());
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload)
                };

                if (configureRequestAsync is not null)
                {
                    await configureRequestAsync(request);
                }

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                else
                    return await response.Content.ReadFromJsonAsync<Error>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API Request Failed: {ex.Message}");
                return new Error(ErrorCodes.Client.NetworkError, _localizer[ErrorCodes.Client.NetworkError]);
            }
        }
    }
}
