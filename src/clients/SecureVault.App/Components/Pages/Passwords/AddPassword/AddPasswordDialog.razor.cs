using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Nager.PublicSuffix;
using SecureVault.App.Application.Features.CQRS.Passwords.Commands;
using SecureVault.App.Models.PassowordModels;
using SecureVault.App.Resources.Localization;

namespace SecureVault.App.Components.Pages.Passwords.AddPassword;

public partial class AddPasswordDialog
{
    private MudForm form;
    private bool _submitting = false;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] private IMapper Mapper { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; }
    [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; }
    [Inject] private ILogger<AddPasswordDialog> Logger { get; set; } = null!;
    [Inject] private IDomainParser DomainParser { get; set; } = default!;

    private CreatePasswordModel createPasswordModel = new();

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
            var command = Mapper.Map<CreatePasswordCommand>(createPasswordModel);
            var result = await Mediator.Send(command);
            if (result.IsSuccess)
            {
                Snackbar.Add(Localizer[SharedResources.TwoFactorAddedSuccessfully], Severity.Success);
                MudDialog.Close(DialogResult.Ok(createPasswordModel));
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
        if (string.IsNullOrWhiteSpace(createPasswordModel.SiteUrl))
            return true;


        try
        {
            var tempUrl = createPasswordModel.SiteUrl;
            if (!tempUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                tempUrl = $"https://{tempUrl}";
            }

            var uri = new Uri(tempUrl);
            var domainInfo = DomainParser.Parse(uri.Host);

            createPasswordModel.SiteName = domainInfo.RegistrableDomain ?? uri.Host;
            createPasswordModel.SiteUrl = $"{uri.Scheme}://{uri.Host}";

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "URL parse etme hatası: {Url}", createPasswordModel.SiteUrl);
            return false;
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
