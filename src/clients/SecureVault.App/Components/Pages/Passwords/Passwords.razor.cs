using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using SecureVault.App.Application.Features.CQRS.Passwords.Queries;
using SecureVault.App.Application.Services;
using SecureVault.App.Models.PassowordModels;
using SecureVault.App.Resources.Localization;

namespace SecureVault.App.Components.Pages.Passwords
{
    public partial class Passwords : ComponentBase, IDisposable
    {
        private List<PasswordViewModel> _allItems = [];
        private bool isLoading = true;
        private string? loadError = null;
        private string searchString = "";

        private IEnumerable<IGrouping<string, PasswordViewModel>> filteredAndGroupedItems =>
            _allItems
                .Where(item =>
                    string.IsNullOrWhiteSpace(searchString) ||
                    item.Model.SiteName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    item.Model.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrWhiteSpace(item.Model.Notes) && item.Model.Notes.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item.Model.SiteName)
                .OrderBy(group => group.Key);

        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private IMapper Mapper { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            await InitializePasswords();
            UINotificationService.OnVaultDataChangedAsync += HandleItemAdded;
        }

        private async Task InitializePasswords()
        {
            isLoading = true;
            loadError = string.Empty;
            var result = await Mediator.Send(new GetAllPasswordsQuery());
            if (result is not null)
            {
                var passwordModel = Mapper.Map<List<PasswordModel>>(result);
                _allItems = [.. passwordModel.Select(model => new PasswordViewModel { Model = model })];
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
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            UINotificationService.OnVaultDataChangedAsync -= HandleItemAdded;
        }
    }
}
