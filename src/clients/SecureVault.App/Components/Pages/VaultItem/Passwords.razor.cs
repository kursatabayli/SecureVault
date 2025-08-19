using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SecureVault.App.Services.Models.VaultItemModels;
using SecureVault.App.Services.Resources;
using SecureVault.App.Services.Service.Infrastructure.Contracts;

namespace SecureVault.App.Components.Pages.VaultItem
{
    public partial class Passwords : ComponentBase
    {
        private List<PasswordViewModel> _allItems = new();
        private bool isLoading = true;
        private string? loadError = null;
        private string searchString = "";

        private IEnumerable<IGrouping<string, PasswordViewModel>> filteredAndGroupedItems =>
            _allItems
                .Where(item =>
                    string.IsNullOrWhiteSpace(searchString) ||
                    item.Model.SiteName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    item.Model.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(item.Model.Notes) && item.Model.Notes.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(item => item.Model.SiteName)
                .OrderBy(group => group.Key);

        [Inject] private IVaultItemService<PasswordModel> PasswordVaultService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await InitializePasswords();
        }

        private async Task InitializePasswords()
        {
            isLoading = true;
            loadError = string.Empty;
            var result = await PasswordVaultService.GetVaultItemsByItemTypeAsync(ItemType.Password);
            if (result.IsSuccess)
            {
                _allItems = [.. result.Value.Select(model => new PasswordViewModel { Model = model })];
            }
            else
            {
                loadError = result.Error.Message;
            }
            isLoading = false;
        }

        private void TogglePasswordVisibility(PasswordViewModel item)
        {
            item.IsPasswordVisible = !item.IsPasswordVisible;
        }

        private async Task CopyToClipboard(string text, string objectName)
        {
            if (!string.IsNullOrEmpty(text))
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                Snackbar.Add(Localizer[SharedResources.Copied], Severity.Success, config => { config.VisibleStateDuration = 2000; });
            }
        }

        private string GetAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "#";

            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return url;

            return $"https://{url}";
        }
        private async Task HandleItemAdded()
        {
            await InitializePasswords();
            StateHasChanged();
        }
    }
}
