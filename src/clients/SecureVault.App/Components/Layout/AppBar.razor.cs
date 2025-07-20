using Microsoft.AspNetCore.Components;

namespace SecureVault.App.Components.Layout
{
    public partial class AppBar : ComponentBase
    {
        private bool _drawerOpen = true;

        // Butona tıklandığında bu metot çalışarak _drawerOpen değerini tersine çevirir.
        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }
    }
}
