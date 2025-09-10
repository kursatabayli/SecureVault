using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SecureVault.App.Components.Pages.Passwords.AddPassword
{
    public partial class AddPasswordFab : ComponentBase
    {
        [Parameter] public EventCallback OnItemAdded { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        private async Task OpenAddPasswordDialog()
        {
            var dialog = await DialogService.ShowAsync<AddPasswordDialog>();
            var result = await dialog.Result;
            if (!result.Canceled)
                await OnItemAdded.InvokeAsync();
        }
    }
}
