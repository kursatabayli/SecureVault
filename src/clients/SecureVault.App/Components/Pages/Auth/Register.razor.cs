using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Components.Pages.Auth
{
    public partial class Register : ComponentBase
    {
        MudForm? form;
        private bool Submitting = false;
        private string? Password;
        private string? ConfirmPassword;

        private RegisterUserModel registerModel { get; set; } = new()
        {
            UserInfo = new()
        };
        [Inject] private IBouncyCastleCryptoService CryptoService { get; set; }
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;
        [Inject] private IAuthService AuthService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!; 
        [Inject] private ILogger<Register> Logger { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        private async Task Submit()
        {
            if (form is null) return;
            await form.Validate();
            if (!form.IsValid) return;

            Submitting = true;

            try
            {
                var (salt, publicKey) = CryptoService.GenerateValidKeyPair(Password);
                registerModel.Salt = salt;
                registerModel.PublicKey = publicKey;

                var result = await AuthService.RegisterAsync(registerModel);

                if (result.IsSuccess)
                {
                    Snackbar.Add(Localizer[SharedResources.RegistrationSuccessful], Severity.Success);
                    NavigationManager.NavigateTo("/", true);
                }
                else
                {
                    Snackbar.Add(result.Error.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Register sayfasında beklenmedik bir UI hatası oluştu.");
                Snackbar.Add(Localizer[SharedResources.UnexpectedError], Severity.Error);
            }
            finally
            {
                Submitting = false;
            }
        }
    }
}
