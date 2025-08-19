using Refit;
using SecureVault.App.Services.APIs;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Service.Infrastructure.Contracts;
using SecureVault.Shared.Result;

namespace SecureVault.App.Services.Service.Infrastructure.Implementations
{
    public class RegisterService : IRegisterService
    {
        private readonly ISecureVaultApi _secureVaultApi;

        public RegisterService(ISecureVaultApi secureVaultApi)
        {
            _secureVaultApi = secureVaultApi;
        }

        public async Task<Result?> RegisterAsync(RegisterUserModel registerUserModel)
        {
            try
            {
                var response = await _secureVaultApi.RegisterAsync(registerUserModel);
                if (response.IsSuccessStatusCode)
                    return Result.Success();

                var error = await response.Error.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.RegisterFailed", "Kayıt başarısız."));
            }
            catch (ApiException ex)
            {
                var error = await ex.GetContentAsAsync<Error>();
                return Result.Failure(error ?? new Error("Client.NetworkError", "Bir hata oluştu."));
            }
        }
    }
}
