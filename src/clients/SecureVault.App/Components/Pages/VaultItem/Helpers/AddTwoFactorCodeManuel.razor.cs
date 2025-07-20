using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Components.Pages.VaultItem.Helpers
{
    public partial class AddTwoFactorCodeManuel : ComponentBase
    {
        private MudForm form; 
        private bool _submitting = false;

        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
        [Inject] private IVaultItemService<TwoFactorAuthModel> VaultItemService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private ILogger<AddTwoFactorCodeManuel> Logger { get; set; } = null!;
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;
        private TwoFactorAuthModel _twoFactorAuthModel = new();

        protected override void OnInitialized()
        {
            DialogStyles();
        }
        private async Task Submit()
        {
            if (form is null) return;
            await form.Validate();
            if (!form.IsValid) return;

            _submitting = true;

            try
            {
                var result = await VaultItemService.CreateVaultItem(_twoFactorAuthModel, ItemType.TwoFactorAuth);

                if (result.IsSuccess)
                {
                    Snackbar.Add(Localizer[SharedResources.TwoFactorAddedSuccessfully], Severity.Success);
                    MudDialog.Close(DialogResult.Ok(_twoFactorAuthModel));
                }
                else
                {
                    Snackbar.Add(result.Error.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "2FA kodu ekleme dialogunda beklenmedik bir hata oluştu.");
                Snackbar.Add(Localizer[SharedResources.UnexpectedError], Severity.Error);
            }
            finally
            {
                _submitting = false;
            }
        }

        private void Cancel() => MudDialog.Cancel();

        private Task DialogStyles()
        {
            var options = MudDialog.Options with
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
            };
            return MudDialog.SetOptionsAsync(options);
        }
    }
}
