using Microsoft.Maui.Storage;

namespace StudentApp.Services
{
    public class AppStateService : IAppStateService
    {

        private const string CurrentRouteKey = "app_state_current_route";

        public string? CurrentRoute { get; set; }

        public void SaveState()
        {
            if (!string.IsNullOrEmpty(CurrentRoute))
            {
                Preferences.Default.Set(CurrentRouteKey, CurrentRoute);
                System.Diagnostics.Debug.WriteLine(
                    $"[AppStateService] Збережено стан. CurrentRoute = {CurrentRoute}");
            }
        }

        public void RestoreState()
        {
            CurrentRoute = Preferences.Default.Get<string?>(CurrentRouteKey, null);
            System.Diagnostics.Debug.WriteLine(
                $"[AppStateService] Відновлено стан. CurrentRoute = {CurrentRoute}");
        }
    }
}
