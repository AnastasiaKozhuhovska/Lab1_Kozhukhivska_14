using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace StudentApp.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {

        public const string ThemePreferenceKey = "app_theme";

        private AppTheme _currentTheme;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLightSelected));
                    OnPropertyChanged(nameof(IsDarkSelected));
                }
            }
        }

        public bool IsLightSelected => CurrentTheme == AppTheme.Light;
        public bool IsDarkSelected => CurrentTheme == AppTheme.Dark;

        public ICommand SetThemeCommand { get; }

        public SettingsViewModel()
        {
            SetThemeCommand = new Command<string>(SetTheme);


            var current = Application.Current?.UserAppTheme ?? AppTheme.Unspecified;
            CurrentTheme = current == AppTheme.Unspecified ? AppTheme.Light : current;
        }

        private void SetTheme(string themeName)
        {
            if (!Enum.TryParse<AppTheme>(themeName, out var theme))
                return;

            CurrentTheme = theme;

            if (Application.Current is not null)
            {
                Application.Current.UserAppTheme = theme;
            }

            Preferences.Default.Set(ThemePreferenceKey, theme.ToString());

            System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Тему змінено на: {theme}");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
