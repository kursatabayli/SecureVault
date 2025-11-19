using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.App.Services.Interfaces;

namespace SecureVault.App.Components.Pages.TwoFactorAuthCodes.AddTwoFactorCode;

public partial class AddTwoFactorCodeManuel : ComponentBase
{
    private MudForm form;
    private bool _submitting = false;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }
    [Inject] private ITwoFactorAuthCodeCreationService CreationService { get; set; } = null!;
    private TwoFactorAuthCodeModel _twoFactorAuthModel = new();

    protected override void OnInitialized()
    {
        DialogStyles();
    }
    private async Task Submit()
    {
        await form.Validate();
        if (!form.IsValid) return;

        _submitting = true;

        var success = await CreationService.SubmitCreationCommandAsync(_twoFactorAuthModel, "Manuel");
        if (success)
        {
            MudDialog.Close(DialogResult.Ok(_twoFactorAuthModel));
        }

        _submitting = false;
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
