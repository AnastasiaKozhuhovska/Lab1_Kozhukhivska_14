using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using StudentApp.Data;
using StudentApp.Models;

namespace StudentApp.ViewModels
{
    public class StudentDetailViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        private readonly IStudentRepository _repository;
        private readonly ILogger<StudentDetailViewModel> _logger;

        // Працюємо з копією, щоб без натискання "Зберегти" зміни не потрапляли у список.
        private Student _student = new();

        public string FullName
        {
            get => _student.FullName;
            set
            {
                if (_student.FullName != value)
                {
                    _student.FullName = value;
                    OnPropertyChanged();
                    ((Command)SaveCommand).ChangeCanExecute();
                }
            }
        }

        public string Group
        {
            get => _student.Group;
            set
            {
                if (_student.Group != value)
                {
                    _student.Group = value;
                    OnPropertyChanged();
                }
            }
        }

        public double AverageScore
        {
            get => _student.AverageScore;
            set
            {
                if (Math.Abs(_student.AverageScore - value) > 0.0001)
                {
                    _student.AverageScore = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Номер студентського квитка (у БД зберігається зашифрованим).</summary>
        public string StudentCardNumber
        {
            get => _student.StudentCardNumber ?? string.Empty;
            set
            {
                if (_student.StudentCardNumber != value)
                {
                    _student.StudentCardNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SelectedStudent", out var value) && value is Student student)
            {
                _student = new Student
                {
                    Id = student.Id,
                    FullName = student.FullName,
                    Group = student.Group,
                    AverageScore = student.AverageScore,
                    CreatedAt = student.CreatedAt,
                    StudentCardNumber = student.StudentCardNumber
                };

                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Group));
                OnPropertyChanged(nameof(AverageScore));
                OnPropertyChanged(nameof(StudentCardNumber));
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }

        public ICommand GoBackCommand { get; }

        /// <summary>Зберігає зміни студента в БД через репозиторій.</summary>
        public ICommand SaveCommand { get; }

        public StudentDetailViewModel(IStudentRepository repository, ILogger<StudentDetailViewModel> logger)
        {
            _repository = repository;
            _logger = logger;

            GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            SaveCommand = new Command(async () => await SaveAsync(),
                                      () => !string.IsNullOrWhiteSpace(FullName));
        }

        private async Task SaveAsync()
        {
            // Перевірка даних перед збереженням: некоректне значення — Warning, запис не зберігається.
            if (_student.AverageScore is < 0 or > 5)
            {
                _logger.LogWarning("Зміни студента {StudentId} не збережено: некоректний середній бал {AverageScore}",
                    _student.Id, _student.AverageScore);
                return;
            }

            try
            {
                // Id != 0 → UpdateAsync, Id == 0 (наприклад, студент із deep link) → InsertAsync.
                var rows = await _repository.SaveAsync(_student);

                if (rows == 0)
                {
                    _logger.LogWarning("Зміни студента {StudentId} не збережено: запис не знайдено в БД", _student.Id);
                    return;
                }

                _logger.LogInformation("Зміни студента {StudentId} ({FullName}) успішно збережено",
                    _student.Id, _student.FullName);

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка збереження змін студента {StudentId}", _student.Id);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
