using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using SecureVault.App.Services;
using SecureVault.App.Services.Models.RecoveryKeyModels;
using SecureVault.App.Services.Models.RegisterModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Application.Contracts;

namespace SecureVault.App.Components.Pages.Register
{
    public partial class Register : ComponentBase
    {
        private bool _submitting = false;
        private int _registrationStep = 1;
        private string _loadingText = "İşlem yapılıyor...";
        private string _animationClass = "slide-in-right";

        private RegisterUserModel? _registerModel;
        private string? _password;
        private RecoveryKeyModel? _recoveryKey;

        [Inject] private IUserRegistrationService UserRegistrationService { get; set; } = null!;
        [Inject] private IBip39RecoveryKeyService Bip39RecoveryKeyService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        [Inject] private ILogger<Register> Logger { get; set; } = null!;
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;

        private async Task ProceedToRecoveryStep((RegisterUserModel model, string password) args)
        {
            _registerModel = args.model;
            _password = args.password;

            _animationClass = "slide-out-left";
            StateHasChanged();
            await Task.Delay(400);

            _recoveryKey = Bip39RecoveryKeyService.Generate();
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
            _recoveryKey = null;
            _animationClass = "slide-in-left";
            StateHasChanged();
        }
        private async Task Submit()
        {
            if (_registerModel is null || _recoveryKey is null || string.IsNullOrEmpty(_password))
            {
                Snackbar.Add("Beklenmedik bir hata oluştu. Lütfen sayfayı yenileyin.", Severity.Error);
                return;
            }

            _submitting = true;
            _loadingText = "Hesabınız oluşturuluyor...";
            StateHasChanged();

            try
            {
                var result = await UserRegistrationService.RegisterAndBackupAsync(_registerModel, _password, _recoveryKey);

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
            _recoveryKey = null;
            _animationClass = "slide-in-left";
            StateHasChanged();
        }
    }
}