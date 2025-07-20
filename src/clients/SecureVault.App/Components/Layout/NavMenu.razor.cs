using Microsoft.AspNetCore.Components;

namespace SecureVault.App.Components.Layout
{
    public partial class NavMenu : ComponentBase
    {
        private bool _drawerOpen = false;
        private void DrawerToggle() => _drawerOpen = !_drawerOpen;
    }
}
