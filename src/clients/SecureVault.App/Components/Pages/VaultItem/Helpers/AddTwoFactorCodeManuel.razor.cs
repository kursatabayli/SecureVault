using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Service.Contracts;

namespace SecureVault.App.Components.Pages.VaultItem.Helpers
{
    public partial class AddTwoFactorCodeManuel : ComponentBase
    {
        private MudForm form;
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
        [Inject] private IVaultItemService<TwoFactorAuthModel> VaultItemService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        private TwoFactorAuthModel _twoFactorAuthModel = new();

        protected override void OnInitialized()
        {
            DialogStyles();
        }
        private async Task Submit()
        {
            if (!await ValidateFormAsync())
                return;

            var result = await VaultItemService.CreateVaultItem(_twoFactorAuthModel, ItemType.TwoFactorAuth);
            if (result)
            {
                MudDialog.Close(DialogResult.Ok(_twoFactorAuthModel));
                Snackbar.Add("İki faktörlü kimlik doğrulama kodu başarıyla eklendi.", Severity.Success);
            }
            else
            {
                Snackbar.Add("İki faktörlü kimlik doğrulama kodu eklenemedi.", Severity.Error);
            }
            Cancel();

        }
        private async Task<bool> ValidateFormAsync()
        {
            await form.Validate();
            return form.IsValid;
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
