using AutoMapper;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using Realms;
using SecureVault.App.Application.Contracts.Repositories;
using SecureVault.App.Domain.Entities;
using SecureVault.App.Models.PassowordModels;
using SecureVault.App.Resources.Localization;

namespace SecureVault.App.Components.Pages.Passwords;

public partial class Passwords : ComponentBase, IDisposable
{
    private List<PasswordViewModel> _allItems = [];
    private bool isLoading = true;
    private string? loadError = null;
    private string searchString = "";
    private IRealmCollection<PasswordEntity> _liveData;
    private IDisposable _notificationToken;

    private IEnumerable<IGrouping<string, PasswordViewModel>> filteredAndGroupedItems =>
        _allItems
            .Where(item =>
                string.IsNullOrWhiteSpace(searchString) ||
                item.Model.SiteName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                item.Model.Username.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(item.Model.Notes) && item.Model.Notes.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => item.Model.SiteName)
            .OrderBy(group => group.Key);

    [Inject] private IPasswordRepository Repository { get; set; } = default!;
    [Inject] private IMapper Mapper { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IStringLocalizer<SharedResources> Localizer { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        try
        {
            _liveData = await Repository.GetLiveCollectionAsync();
            MapDataToViewModel(_liveData);
            _notificationToken = _liveData.SubscribeForNotifications(OnDataChanged);
            isLoading = false;
        }
        catch (Exception ex)
        {
            loadError = $"Veri yüklenemedi: {ex.Message}";
            isLoading = false;
        }
    }
    private void OnDataChanged(IRealmCollection<PasswordEntity> sender, ChangeSet? changes)
    {
        MapDataToViewModel(sender);
        InvokeAsync(StateHasChanged);
    }

    private void MapDataToViewModel(IRealmCollection<PasswordEntity> data)
    {
        var passwordModel = Mapper.Map<List<PasswordModel>>(data);
        _allItems = [.. passwordModel.Select(model => new PasswordViewModel { Model = model })];
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

    public void Dispose()
    {
        _notificationToken?.Dispose();
    }
}
