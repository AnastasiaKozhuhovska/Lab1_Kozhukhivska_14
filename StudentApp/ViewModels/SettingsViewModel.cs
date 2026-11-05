using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Serilog;
using StudentApp.Logging;

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

        // ===== Діагностика: моделювання необроблених винятків =====

        private string _diagnosticsStatus = string.Empty;
        private string _logTail = string.Empty;

        public string LogDirectory => LoggingSetup.LogDirectory;

        public string DiagnosticsStatus
        {
            get => _diagnosticsStatus;
            private set { _diagnosticsStatus = value; OnPropertyChanged(); }
        }

        public string LogTail
        {
            get => _logTail;
            private set { _logTail = value; OnPropertyChanged(); }
        }

        /// <summary>Виняток у фоновій Task, яку ніхто не очікує → TaskScheduler.UnobservedTaskException.</summary>
        public ICommand SimulateBackgroundTaskExceptionCommand { get; }

        /// <summary>Виняток у фоновому потоці → AppDomain.UnhandledException, застосунок завершується.</summary>
        public ICommand SimulateFatalExceptionCommand { get; }

        /// <summary>Показує останні рядки поточного лог-файлу.</summary>
        public ICommand ShowLogTailCommand { get; }

        /// <summary>Відкриває екран перегляду локальних логів.</summary>
        public ICommand OpenLogViewerCommand { get; }

        /// <summary>Надсилає в Sentry тестову (некритичну) помилку.</summary>
        public ICommand SendTestErrorToSentryCommand { get; }

        public string SentryStatus => CrashReportingSettings.IsConfigured
            ? "Sentry: увімкнено ✅"
            : "Sentry: вимкнено — вкажіть DSN у Logging/CrashReportingSettings.cs";

        public SettingsViewModel()
        {
            SetThemeCommand = new Command<string>(SetTheme);
            SimulateBackgroundTaskExceptionCommand = new Command(async () => await SimulateBackgroundTaskExceptionAsync());
            SimulateFatalExceptionCommand = new Command(SimulateFatalException);
            ShowLogTailCommand = new Command(async () => await ShowLogTailAsync());
            SendTestErrorToSentryCommand = new Command(SendTestErrorToSentry);
            OpenLogViewerCommand = new Command(async () => await Shell.Current.GoToAsync("logs"));


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

        private async Task SimulateBackgroundTaskExceptionAsync()
        {
            Log.Warning("МОДЕЛЮВАННЯ: запуск фонової задачі, що завершиться необробленим винятком");
            DiagnosticsStatus = "Фонова задача запущена...";

            StartFaultyBackgroundTask();

            // Даємо задачі час впасти.
            await Task.Delay(500);

            // UnobservedTaskException спрацьовує, коли збирач сміття знищує "забуту" задачу.
            // Щоб не чекати, викликаємо збирання сміття примусово.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            await Task.Delay(300); // час на запис у лог-файл

            DiagnosticsStatus = "Готово: застосунок продовжує працювати. Перевірте лог (запис [ERR]).";
            await ShowLogTailAsync();
        }

        // Окремий метод, щоб на задачу не лишилося жодного посилання (інакше GC її не збере).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartFaultyBackgroundTask()
        {
            // Задачу запущено і "забуто": немає ні await, ні .Wait(), ні try/catch.
            _ = Task.Run(ThrowInBackground);
        }

        private static void ThrowInBackground() =>
            throw new InvalidOperationException("Тестовий збій у фоновій задачі (моделювання)");

        private static void SimulateFatalException()
        {
            Log.Warning("МОДЕЛЮВАННЯ: необроблений виняток у фоновому потоці — застосунок буде завершено");

            // Виняток у звичайному потоці (Thread) ніхто не може перехопити →
            // спрацьовує AppDomain.UnhandledException, після чого процес завершується.
            var thread = new Thread(() =>
                throw new ApplicationException("Тестовий критичний збій у фоновому потоці (моделювання)"))
            {
                IsBackground = true,
                Name = "CrashSimulationThread"
            };
            thread.Start();
        }

        private void SendTestErrorToSentry()
        {
            try
            {
                throw new InvalidOperationException("Тестова помилка для Sentry (моделювання, некритична)");
            }
            catch (Exception ex)
            {
                // Помилку перехоплено, але вручну надсилаємо звіт у Sentry.
                var eventId = global::Sentry.SentrySdk.CaptureException(ex);

                Log.Warning(ex, "Тестову помилку надіслано в Sentry. EventId: {EventId}", eventId);

                DiagnosticsStatus = global::Sentry.SentrySdk.IsEnabled
                    ? $"Надіслано в Sentry. ID події: {eventId}"
                    : "Sentry вимкнено — спершу вкажіть DSN.";
            }
        }

        private async Task ShowLogTailAsync(int lineCount = 15)
        {
            try
            {
                var file = new DirectoryInfo(LoggingSetup.LogDirectory)
                    .GetFiles("studentapp-*.log")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (file is null)
                {
                    LogTail = "Лог-файл ще не створено.";
                    return;
                }

                // Файл відкритий Serilog на запис, тому читаємо з FileShare.ReadWrite.
                using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read,
                                                  FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var lines = (await reader.ReadToEndAsync())
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.TrimEnd('\r'));

                // Показуємо лише рядки з рівнем логу (без рядків стеку викликів).
                var tail = lines.Where(l => l.Contains(" [")).TakeLast(lineCount);

                LogTail = $"{file.Name}:\n" + string.Join("\n", tail);
            }
            catch (Exception ex)
            {
                LogTail = $"Не вдалося прочитати лог: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
