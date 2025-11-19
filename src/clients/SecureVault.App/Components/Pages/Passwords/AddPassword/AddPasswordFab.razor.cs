using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SecureVault.App.Components.Pages.Passwords.AddPassword;

public partial class AddPasswordFab : ComponentBase
{
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private ISnackbar Snackbar { get; set; }

    private async Task OpenAddPasswordDialog()
    {
        var dialog = await DialogService.ShowAsync<AddPasswordDialog>();
        var result = await dialog.Result;
        if (!result.Canceled && result.Data is bool success && success)
            Snackbar.Add("Şifre başarıyla eklendi.", Severity.Success);
    }
}
