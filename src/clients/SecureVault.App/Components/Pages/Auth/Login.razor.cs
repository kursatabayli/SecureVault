using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Components.Pages.Auth
{
    public partial class Login : ComponentBase
    {
        private MudForm? form;
        private bool Submitting = false;
        private readonly LoginModel loginModel = new();
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;
        [Inject] private IAuthService AuthService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ILogger<Login> Logger { get; set; } = null!;

        private async Task Submit()
        {
            if (form is null) return;
            await form.Validate();
            if (!form.IsValid) return;

            Submitting = true;

            try
            {
                var result = await AuthService.LoginAsync(loginModel);

                if (result.IsSuccess)
                {
                    NavigationManager.NavigateTo("/", true);
                }
                else
                {
                    Snackbar.Add(result.Error.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Login sayfasında beklenmedik bir hata oluştu.");
                Snackbar.Add(Localizer[SharedResources.UnexpectedError], Severity.Error);
            }
            finally
            {
                Submitting = false;
            }
        }
    }
}