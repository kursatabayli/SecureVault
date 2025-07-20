using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services.Models.RegisterModels;
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
        [Inject] private IAuthService AuthService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;

        private async Task Submit()
        {
            if (!await ValidateFormAsync())
                return;

            try
            {
                Submitting = true;
                await HandleRegistration();
            }
            finally
            {
                Submitting = false;
            }
        }

        private async Task<bool> ValidateFormAsync()
        {
            if (form is null) return false;

            await form.Validate();
            return form.IsValid;
        }

        private async Task HandleRegistration()
        {
            try
            {
                var (salt, publicKey) = CryptoService.GenerateValidKeyPair(Password);
                registerModel.Salt = salt;
                registerModel.PublicKey = publicKey;

                var result = await AuthService.RegisterAsync(registerModel);
                if (result.IsSuccess)
                {
                    NavigationManager.NavigateTo("/", true);
                    Snackbar.Add("başarılı", Severity.Success);
                }
                else
                {
                    Snackbar.Add(result.Error.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Kayıt işlemi sırasında bir hata oluştu: {ex.InnerException.StackTrace}", Severity.Error);
            }

        }
    }
}
