using Refit;
using SecureVault.App.Application.Contracts.Abstractions.Api;
using SecureVault.App.Application.Contracts.DTOs.Register;
using SecureVault.Shared.Result;

namespace SecureVault.App.Infrastructure.Services.Api
{
    public class RegisterService : IRegisterService
    {
        private readonly ISecureVaultAnonymousApi _secureVaultAnonymousApi;

        public RegisterService(ISecureVaultAnonymousApi secureVaultAnonymousApi)
        {
            _secureVaultAnonymousApi = secureVaultAnonymousApi;
        }

        public async Task<Result?> RegisterAsync(RegisterUserDto registerUserDto, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _secureVaultAnonymousApi.RegisterAsync(registerUserDto, cancellationToken);
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
