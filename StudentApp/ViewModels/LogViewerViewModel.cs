using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using StudentApp.Logging;
using StudentApp.Models;
using StudentApp.Services.Logs;

namespace StudentApp.ViewModels
{
    public class LogViewerViewModel : INotifyPropertyChanged
    {
        private const string AllLevels = "Усі рівні";

        private readonly ILogReaderService _logReader;
        private readonly ILogger<LogViewerViewModel> _logger;

        private string _selectedLevel = AllLevels;
        private int _selectedCount = 100;
        private bool _isBusy;
        private string _summary = string.Empty;

        public LogViewerViewModel(ILogReaderService logReader, ILogger<LogViewerViewModel> logger)
        {
            _logReader = logReader;
            _logger = logger;
            LoadCommand = new Command(async () => await LoadAsync());
        }

        /// <summary>Записи для CollectionView (від нових до старих).</summary>
        public ObservableCollection<LogEntry> Entries { get; } = new();

        /// <summary>Варіанти фільтра за рівнем.</summary>
        public IReadOnlyList<string> LevelOptions { get; } =
            new[] { AllLevels, "Debug", "Information", "Warning", "Error", "Fatal" };

        /// <summary>Варіанти кількості записів N.</summary>
        public IReadOnlyList<int> CountOptions { get; } = new[] { 50, 100, 200, 500 };

        public string SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                if (_selectedLevel == value || value is null) return;
                _selectedLevel = value;
                OnPropertyChanged();
                LoadCommand.Execute(null); // зміна фільтра — одразу перечитуємо
            }
        }

        public int SelectedCount
        {
            get => _selectedCount;
            set
            {
                if (_selectedCount == value) return;
                _selectedCount = value;
                OnPropertyChanged();
                LoadCommand.Execute(null);
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        public string Summary
        {
            get => _summary;
            private set { _summary = value; OnPropertyChanged(); }
        }

        public string LogDirectory => LoggingSetup.LogDirectory;

        public ICommand LoadCommand { get; }

        private async Task LoadAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var levelCode = ToLevelCode(SelectedLevel);
                Func<LogEntry, bool>? filter = levelCode is null ? null : e => e.Level == levelCode;

                var entries = await _logReader.ReadLatestAsync(SelectedCount, filter);

                Entries.Clear();
                foreach (var entry in entries)
                    Entries.Add(entry);

                Summary = $"Показано {Entries.Count} з макс. {SelectedCount} · рівень: {SelectedLevel}";

                _logger.LogDebug("Переглядач логів: показано {Count} записів, фільтр {Level}",
                    Entries.Count, SelectedLevel);
            }
            catch (Exception ex)
            {
                Summary = $"Не вдалося прочитати логи: {ex.Message}";
                _logger.LogError(ex, "Помилка читання лог-файлів");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Назва рівня → скорочення у файлі (формат {Level:u3}).</summary>
        private static string? ToLevelCode(string level) => level switch
        {
            "Debug" => "DBG",
            "Information" => "INF",
            "Warning" => "WRN",
            "Error" => "ERR",
            "Fatal" => "FTL",
            _ => null // "Усі рівні"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
