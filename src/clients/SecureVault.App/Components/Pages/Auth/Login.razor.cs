using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Application.Features.CQRS.Auth.Commands;
using SecureVault.App.Models.LoginModels;
using SecureVault.App.Resources.Localization;

namespace SecureVault.App.Components.Pages.Auth
{
    public partial class Login : ComponentBase
    {
        private MudForm? form;
        private bool Submitting = false;
        private LoginModel loginModel = new();
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;
        [Inject] private IMediator Mediator { get; set; } = null!;
        [Inject] private IMapper Mapper { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ILogger<Login> Logger { get; set; } = null!;

        private async Task Submit()
        {
            if (form is null) return;
            await form.Validate();
            if (!form.IsValid) return;

            Submitting = true;
            await Task.Yield();

            try
            {
                var command = Mapper.Map<LoginCommand>(loginModel);
                var result = await Mediator.Send(command);

                if (result.IsSuccess)
                    NavigationManager.NavigateTo("/", true);
                else
                {
                    Submitting = false;
                    Snackbar.Add(result.Error.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Submitting = false;
                Logger.LogError(ex, "Login sayfasında beklenmedik bir hata oluştu.");
                Snackbar.Add(Localizer[SharedResources.UnexpectedError], Severity.Error);
            }
        }
    }
}