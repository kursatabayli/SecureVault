using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.Cryptography;
using SecureVault.App.Application.Features.CQRS.Register.Commands;
using SecureVault.App.Models.RecoveryKeyModels;
using SecureVault.App.Models.RegisterModels;
using SecureVault.App.Resources.Localization;

namespace SecureVault.App.Components.Pages.Register
{
    public partial class Register : ComponentBase
    {
        private bool _submitting = false;
        private int _registrationStep = 1;
        private string _loadingText = "İşlem yapılıyor...";
        private string _animationClass = "slide-in-right";

        private RegisterModel _registerModel;
        private RecoveryKeyModel _recoveryKeyModel;
        [Inject] private IMediator Mediator { get; set; } = null!;
        [Inject] private IBip39RecoveryKeyService Bip39RecoveryKeyService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ILogger<Register> Logger { get; set; } = null!;
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;

        private async Task ProceedToRecoveryStep(RegisterModel registerModel)
        {
            _registerModel = registerModel;
            _animationClass = "slide-out-left";
            StateHasChanged();
            await Task.Delay(400);

            _recoveryKeyModel = new RecoveryKeyModel(Bip39RecoveryKeyService.Generate());

            _registrationStep = 2;
            _animationClass = "slide-in-right";
            StateHasChanged();
        }
        private async Task HandleBackNavigation()
        {
            _animationClass = "slide-out-right";
            StateHasChanged();
            await Task.Delay(400);

            _registrationStep = 1;
            _animationClass = "slide-in-left";
            StateHasChanged();
        }
        private async Task Submit()
        {
            if (_registerModel is null)
            {
                Snackbar.Add("Beklenmedik bir hata oluştu. Lütfen sayfayı yenileyin.", Severity.Error);
                return;
            }

            _submitting = true;
            _loadingText = "Hesabınız oluşturuluyor...";
            StateHasChanged();

            try
            {
                var command = new RegisterUserCommand 
                {
                    Email = _registerModel.Email,
                    Password = _registerModel.Password,
                    Name = _registerModel.Name,
                    Surname = _registerModel.Surname,
                    PhoneNumber = _registerModel.PhoneNumber,
                    MnemonicRecoveryKey = _recoveryKeyModel.Mnemonic
                };

                var result = await Mediator.Send(command);
                if (result.IsSuccess)
                {
                    Snackbar.Add("Hesabınız başarıyla oluşturuldu!", Severity.Success);
                    NavigationManager.NavigateTo("/login");
                }
                else
                {
                    Snackbar.Add(result.Error.Message, Severity.Error);
                    await ReturnToFirstStep();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Register sayfasında UI katmanında beklenmedik bir hata oluştu.");
                Snackbar.Add(Localizer[SharedResources.UnexpectedError], Severity.Error);
                await ReturnToFirstStep();
            }
            finally
            {
                _submitting = false;
                StateHasChanged();
            }
        }

        private async Task ReturnToFirstStep()
        {
            _animationClass = "slide-out-right";
            StateHasChanged();
            await Task.Delay(400);

            _registrationStep = 1;
            _recoveryKeyModel = null;
            _animationClass = "slide-in-left";
            StateHasChanged();
        }
    }
}