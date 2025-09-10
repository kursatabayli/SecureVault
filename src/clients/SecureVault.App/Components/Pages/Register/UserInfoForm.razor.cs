using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Features.CQRS.Register.Commands;
using SecureVault.App.Models.RegisterModels;

namespace SecureVault.App.Components.Pages.Register
{
    public partial class UserInfoForm : ComponentBase
    {
        private MudForm? _form;
        private string? _confirmPassword;
        private readonly RegisterModel _registerModel = new();

        [Inject] private ISnackbar Snackbar { get; set; } = null!;

        [Parameter] public EventCallback<RegisterModel> OnProceed { get; set; }

        private async Task HandleProceed()
        {
            if (_form is null) return;
            await _form.Validate();

            if (!_form.IsValid)
            {
                Snackbar.Add("Lütfen tüm alanları doğru bir şekilde doldurun.", Severity.Warning);
                return;
            }

            if (_registerModel.Password != _confirmPassword)
            {
                Snackbar.Add("Şifreler uyuşmuyor.", Severity.Warning);
                return;
            }

            if (OnProceed.HasDelegate)
            {
                await OnProceed.InvokeAsync(_registerModel);
            }
        }
    }
}
