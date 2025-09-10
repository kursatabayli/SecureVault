using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SecureVault.App.Application.Contracts.Abstractions.Persistence;
using SecureVault.App.Application.Features.CQRS.Sessions.Commands;
using SecureVault.App.Application.Features.CQRS.Sessions.Queries;
using SecureVault.App.Models.SessionModels;
using Color = MudBlazor.Color;

namespace SecureVault.App.Components.Pages.Settings.Sessions
{
    public partial class Sessions : ComponentBase
    {
        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private IMapper Mapper { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IStorageService StorageService { get; set; } = default!;

        private List<UserSessionsModel> _sessions = [];
        private bool _isLoading = true;
        private string? _loadError;
        private string? _uniqueDeviceId;
        private Guid? _revokingSessionId;
        protected override async Task OnInitializedAsync()
        {
            _uniqueDeviceId = await StorageService.GetUniqueDeviceIdAsync();
            await LoadSessionsAsync();
        }

        private async Task LoadSessionsAsync()
        {
            _isLoading = true;
            try
            {
                var result = await Mediator.Send(new GetUserSessionsQuery());
                if (result.IsSuccess)
                    _sessions = Mapper.Map<List<UserSessionsModel>>(result.Value);
                else
                    _loadError = result.Error.Message;

                _sessions = [.. _sessions.OrderByDescending(s => IsCurrentSession(s)).ThenByDescending(s => s.LastUsedAt)];
            }
            catch (Exception ex)
            {
                _loadError = $"Oturumlar yüklenirken bir hata oluştu: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task ShowRevokeConfirmation(UserSessionsModel session)
        {
            var parameters = new DialogParameters<ConfirmationDialog>
            {
                { x => x.ContentText, "Bu oturumu sonlandırmak istediğinizden emin misiniz? Bu işlem geri alınamaz." },
                { x => x.ButtonText, "Evet, Sonlandır" },
                { x => x.Color, Color.Error }
            };

            var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Oturumu Sonlandır", parameters);
            var result = await dialog.Result;

            if (result is not null && !result.Canceled)
            {
                await RevokeSession(session);
            }
        }

        private async Task RevokeSession(UserSessionsModel session)
        {
            _revokingSessionId = session.Id;
            StateHasChanged();

            var command = new RevokeUserSessionCommand { SessionId = session.Id };
            var result = await Mediator.Send(command); 
            
            if (result.IsSuccess)
            {
                session.IsRevoked = true;
                Snackbar.Add("Oturum başarıyla sonlandırıldı.", Severity.Success);
            }
            else
            {
                Snackbar.Add($"Oturum sonlandırılamadı: {result.Error.Message}", Severity.Error);
            }

            _revokingSessionId = null;
            StateHasChanged();
        }

        private bool IsCurrentSession(UserSessionsModel session) => session.DeviceDetails.UniqueDeviceId == _uniqueDeviceId;
        private bool IsRecentlyActive(UserSessionsModel session)
        {
            if (IsCurrentSession(session) || session.IsRevoked)
                return false;

            return session.LastUsedAt.HasValue && (DateTimeOffset.UtcNow - session.LastUsedAt.Value).TotalMinutes < 15;
        }

        private string GetDeviceIcon(DeviceDetailModel device)
        {
            var os = device.OperatingSystem?.ToLower() ?? "";
            return os switch
            {
                "android" => Icons.Material.Filled.Smartphone,
                "windows" => Icons.Material.Filled.Computer,
                _ => Icons.Material.Filled.Devices,
            };
        }

        private string FormatDeviceName(DeviceDetailModel device) => string.IsNullOrWhiteSpace(device.DeviceName) ? device.DeviceManufacturer : device.DeviceName;

        private string FormatLastUsed(DateTimeOffset? lastUsedAt)
        {
            if (!lastUsedAt.HasValue) return "Bilinmiyor";

            var diff = DateTimeOffset.UtcNow - lastUsedAt.Value;

            if (diff.TotalMinutes < 1) return "Az önce";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dakika önce";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat önce";
            if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} gün önce";

            return lastUsedAt.Value.ToLocalTime().ToString("dd MMMM yyyy");
        }

        private async Task OpenAuthorizeDeviceDialog()
        {
            var options = new DialogOptions
            {
                BackdropClick = false,
                CloseOnEscapeKey = false,
            };
            var dialog = await DialogService.ShowAsync<AuthorizeDeviceDialog>("Yeni Cihazı Yetkilendir", options);
            var result = await dialog.Result;
            if (!result.Canceled)
                await LoadSessionsAsync();
        }
    }
}
