using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using StudentApp.Data;
using StudentApp.Models;
using StudentApp.Services.Sync;

namespace StudentApp.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly IStudentRepository _repository;
        private readonly ISyncService _syncService;
        private readonly ILogger<StudentViewModel> _logger;

        // Поля форми додавання нового студента.
        private string _fullName = string.Empty;
        private string _group = string.Empty;
        private double _averageScore;
        private string _studentCardNumber = string.Empty;
        private bool _isBusy;

        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GreetingText));

                    ((Command)AddStudentCommand).ChangeCanExecute();
                }
            }
        }

        public string Group
        {
            get => _group;
            set
            {
                if (_group != value)
                {
                    _group = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GreetingText));
                }
            }
        }

        public double AverageScore
        {
            get => _averageScore;
            set
            {
                if (Math.Abs(_averageScore - value) > 0.0001)
                {
                    _averageScore = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AverageScoreColor));
                }
            }
        }

        /// <summary>Номер студентського квитка (чутливе поле, у БД зберігається зашифрованим).</summary>
        public string StudentCardNumber
        {
            get => _studentCardNumber;
            set
            {
                if (_studentCardNumber != value)
                {
                    _studentCardNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Чи виконується зараз операція з БД (для індикатора завантаження).</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    ((Command)AddStudentCommand).ChangeCanExecute();
                }
            }
        }

        public string GreetingText => $"Студент: {FullName}, група {Group}";

        public Color AverageScoreColor =>
            AverageScore >= 4.0 ? Colors.SeaGreen : Colors.OrangeRed;

        public ObservableCollection<Student> Students { get; } = new();

        // ---- Стан мережі та черги офлайн-змін ----

        public bool IsOnline => _syncService.IsOnline;

        public int PendingSyncCount => _syncService.PendingCount;

        public string SyncStatusText => IsOnline
            ? (PendingSyncCount == 0
                ? "🟢 Онлайн · усі зміни синхронізовано"
                : $"🟢 Онлайн · синхронізація… (у черзі: {PendingSyncCount})")
            : $"🔴 Офлайн · у черзі змін: {PendingSyncCount}";

        public Color SyncStatusColor => IsOnline ? Colors.SeaGreen : Colors.OrangeRed;

        /// <summary>Ручний запуск відправлення черги на сервер.</summary>
        public ICommand SyncNowCommand { get; }

        /// <summary>Завантажує список студентів із БД.</summary>
        public ICommand LoadCommand { get; }

        /// <summary>Додає студента з форми в БД.</summary>
        public ICommand AddStudentCommand { get; }

        /// <summary>Відкриває сторінку редагування вибраного студента.</summary>
        public ICommand EditStudentCommand { get; }

        /// <summary>Видаляє вибраного студента з БД.</summary>
        public ICommand DeleteStudentCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public StudentViewModel(IStudentRepository repository, ISyncService syncService,
                                ILogger<StudentViewModel> logger)
        {
            _repository = repository;
            _logger = logger;
            _syncService = syncService;
            _syncService.StatusChanged += OnSyncStatusChanged;

            SyncNowCommand = new Command(async () => await _syncService.ProcessQueueAsync());

            LoadCommand = new Command(async () => await LoadAsync());
            AddStudentCommand = new Command(async () => await AddStudentAsync(), CanAddStudent);
            EditStudentCommand = new Command<Student>(async (student) => await EditStudentAsync(student));
            DeleteStudentCommand = new Command<Student>(async (student) => await DeleteStudentAsync(student));
            OpenSettingsCommand = new Command(async () => await Shell.Current.GoToAsync("settings"));
        }

        private async Task LoadAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Завантаження списку студентів...");

                var students = await _repository.GetAllAsync();

                Students.Clear();
                foreach (var student in students)
                {
                    Students.Add(student);
                }

                _logger.LogInformation("Список студентів завантажено: {Count} запис(ів)", Students.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не вдалося завантажити список студентів");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddStudentAsync()
        {
            var student = new Student
            {
                FullName = FullName.Trim(),
                Group = Group.Trim(),
                AverageScore = AverageScore,
                StudentCardNumber = StudentCardNumber.Trim()
            };

            try
            {
                IsBusy = true;

                var rows = await _repository.SaveAsync(student); // після вставки student.Id заповнено

                if (rows == 0)
                {
                    _logger.LogWarning("Студента {FullName} не додано: БД не змінила жодного рядка", student.FullName);
                    return;
                }

                Students.Add(student);

                _logger.LogInformation("Студента {StudentId} ({FullName}, група {Group}) успішно додано",
                    student.Id, student.FullName, student.Group);

                FullName = string.Empty;
                Group = string.Empty;
                AverageScore = 0;
                StudentCardNumber = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка додавання студента {FullName}", student.FullName);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanAddStudent() => !IsBusy && !string.IsNullOrWhiteSpace(FullName);

        private async Task EditStudentAsync(Student? student)
        {
            if (student is null) return;

            var parameters = new Dictionary<string, object>
            {
                { "SelectedStudent", student }
            };

            // Збереження змін виконує StudentDetailViewModel через репозиторій,
            // а після повернення список перечитується з БД (MainPage.OnAppearing → LoadCommand).
            await Shell.Current.GoToAsync("studentdetail", parameters);
        }

        private async Task DeleteStudentAsync(Student? student)
        {
            if (student is null) return;

            try
            {
                IsBusy = true;

                var rows = await _repository.DeleteAsync(student);
                Students.Remove(student);

                if (rows == 0)
                    _logger.LogWarning("Студента {StudentId} не знайдено в БД під час видалення", student.Id);
                else
                    _logger.LogInformation("Студента {StudentId} успішно видалено", student.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка видалення студента {StudentId}", student.Id);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Подія приходить із фонового потоку, тому оновлюємо UI через MainThread.
        private void OnSyncStatusChanged(object? sender, EventArgs e) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(IsOnline));
                OnPropertyChanged(nameof(PendingSyncCount));
                OnPropertyChanged(nameof(SyncStatusText));
                OnPropertyChanged(nameof(SyncStatusColor));
            });

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
