using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services.Models.RegisterModels;

namespace SecureVault.App.Components.Pages.Register
{
    public partial class UserInfoForm : ComponentBase
    {
        private MudForm? _form;
        private string? _password;
        private string? _confirmPassword;
        private readonly RegisterUserModel _registerModel = new() { UserInfo = new() };

        [Inject] private ISnackbar Snackbar { get; set; } = null!;

        [Parameter] public EventCallback<(RegisterUserModel model, string password)> OnProceed { get; set; }

        private async Task HandleProceed()
        {
            if (_form is null) return;
            await _form.Validate();

            if (!_form.IsValid)
            {
                Snackbar.Add("Lütfen tüm alanları doğru bir şekilde doldurun.", Severity.Warning);
                return;
            }

            if (_password != _confirmPassword)
            {
                Snackbar.Add("Şifreler uyuşmuyor.", Severity.Warning);
                return;
            }

            if (OnProceed.HasDelegate)
            {
                await OnProceed.InvokeAsync((_registerModel, _password!));
            }
        }
    }
}
