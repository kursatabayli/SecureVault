using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services.Models.AuthModels;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Components.Pages.Auth
{
    public partial class Login : ComponentBase
    {
        MudForm? form;
        private bool Submitting = false;
        private LoginModel loginModel { get; set; } = new();
        [Inject] private IAuthService AuthService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }
        private async Task Submit()
        {
            if (!await ValidateFormAsync())
                return;

            try
            {
                Submitting = true;
                await HandleLogin();
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

        private async Task HandleLogin()
        {
            var result = await AuthService.LoginAsync(loginModel);
            if (result.IsSuccess)
                NavigationManager.NavigateTo("/", true);
            else
                Snackbar.Add(result.Error.Message, Severity.Error);
        }
    }
}
