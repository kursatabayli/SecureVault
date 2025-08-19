using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Infrastructure.Contracts;

namespace SecureVault.App.Components.Pages.VaultItem.AddPassword
{
    public partial class AddPasswordDialog
    {
        private MudForm form;
        private bool _submitting = false;
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
        [Inject] private IVaultItemService<PasswordModel> VaultItemService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; }
        [Inject] private ILogger<AddPasswordDialog> Logger { get; set; } = null!;
        [Inject] private IDomainParser DomainParser { get; set; } = default!;

        private PasswordModel newPassword = new();

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
            if (!TryStandardizeUrlAndName())
            {
                Snackbar.Add("Girilen URL geçersiz. Lütfen kontrol edin.", Severity.Warning);
                return;
            }

            try
            {
                var result = await VaultItemService.CreateVaultItem(newPassword, ItemType.Password);

                if (result.IsSuccess)
                {
                    Snackbar.Add(Localizer[SharedResources.TwoFactorAddedSuccessfully], Severity.Success);
                    MudDialog.Close(DialogResult.Ok(newPassword));
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
        private bool TryStandardizeUrlAndName()
        {
            if (string.IsNullOrWhiteSpace(newPassword.SiteUrl))
                return true;


            try
            {
                var tempUrl = newPassword.SiteUrl;
                if (!tempUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    tempUrl = $"https://{tempUrl}";
                }

                var uri = new Uri(tempUrl);
                var domainInfo = DomainParser.Parse(uri.Host);

                newPassword.SiteName = domainInfo.RegistrableDomain ?? uri.Host;
                newPassword.SiteUrl = $"{uri.Scheme}://{uri.Host}";

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "URL parse etme hatası: {Url}", newPassword.SiteUrl);
                return false;
            }
        }

        private async Task UpdateSiteNameFromUrl()
        {

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
