using Microsoft.Maui.Storage;
using StudentApp.Models;
using StudentApp.Services;
using StudentApp.Services.Sync;
using StudentApp.ViewModels;

namespace StudentApp;

public partial class App : Application
{

    private readonly IAppStateService _appStateService;
    private readonly ISyncService _syncService;

    public App(IAppStateService appStateService, ISyncService syncService)
    {
        InitializeComponent();
        _appStateService = appStateService;
        _syncService = syncService;

        // Стежимо за мережею та відправляємо зміни, що лишилися в черзі.
        _syncService.Start();


        var savedTheme = Preferences.Default.Get(
            SettingsViewModel.ThemePreferenceKey,
            AppTheme.Unspecified.ToString());

        if (Enum.TryParse<AppTheme>(savedTheme, out var theme) && theme != AppTheme.Unspecified)
        {
            UserAppTheme = theme;
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        window.Stopped += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("[App] Window Stopped — застосунок згорнуто у фон.");


            _appStateService.CurrentRoute = Shell.Current?.CurrentState?.Location?.ToString();
            _appStateService.SaveState();
        };

        window.Resumed += async (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("[App] Window Resumed — застосунок повернувся на передній план.");

            // Повернулися на передній план — пробуємо відправити чергу офлайн-змін.
            _ = _syncService.ProcessQueueAsync();

            _appStateService.RestoreState();
            var savedRoute = _appStateService.CurrentRoute;
            System.Diagnostics.Debug.WriteLine($"[App] Відновлений маршрут з попередньої сесії: {savedRoute}");

            var currentLocation = Shell.Current?.CurrentState?.Location?.ToString();
            if (!string.IsNullOrEmpty(savedRoute) && savedRoute != currentLocation)
            {
                await Shell.Current!.GoToAsync(savedRoute);
            }
        };

        window.Destroying += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine("[App] Window Destroying — застосунок закривається.");
        };

        return window;
    }

    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        await HandleDeepLinkAsync(uri);
    }

    private static async Task HandleDeepLinkAsync(Uri uri)
    {
        System.Diagnostics.Debug.WriteLine($"[App] Отримано deep link: {uri}");

        if (!string.Equals(uri.Host, "student", StringComparison.OrdinalIgnoreCase))
            return;

        var query = ParseQuery(uri.Query);

        if (!query.TryGetValue("fullname", out var fullName) || string.IsNullOrWhiteSpace(fullName))
            return;

        query.TryGetValue("group", out var group);
        double.TryParse(query.GetValueOrDefault("averagescore"), out var averageScore);

        var student = new Student
        {
            FullName = fullName,
            Group = group ?? string.Empty,
            AverageScore = averageScore
        };

        var parameters = new Dictionary<string, object>
        {
            { "SelectedStudent", student }
        };

        var attempts = 0;
        while (Shell.Current is null && attempts < 50)
        {
            await Task.Delay(100);
            attempts++;
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("studentdetail", parameters);
        }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>();
        query = query.TrimStart('?');

        if (string.IsNullOrEmpty(query))
            return result;

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
