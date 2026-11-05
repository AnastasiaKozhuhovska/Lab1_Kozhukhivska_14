using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using StudentApp.Data;
using StudentApp.Models;

namespace StudentApp.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly IStudentRepository _repository;

        // Поля форми додавання нового студента.
        private string _fullName = string.Empty;
        private string _group = string.Empty;
        private double _averageScore;
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

        /// <summary>Завантажує список студентів із БД.</summary>
        public ICommand LoadCommand { get; }

        /// <summary>Додає студента з форми в БД.</summary>
        public ICommand AddStudentCommand { get; }

        /// <summary>Відкриває сторінку редагування вибраного студента.</summary>
        public ICommand EditStudentCommand { get; }

        /// <summary>Видаляє вибраного студента з БД.</summary>
        public ICommand DeleteStudentCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public StudentViewModel(IStudentRepository repository)
        {
            _repository = repository;

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

                var students = await _repository.GetAllAsync();

                Students.Clear();
                foreach (var student in students)
                {
                    Students.Add(student);
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[StudentViewModel] LoadCommand: з БД завантажено {Students.Count} студент(и/ів).");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudentViewModel] Помилка завантаження: {ex.Message}");
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
                AverageScore = AverageScore
            };

            try
            {
                IsBusy = true;

                await _repository.SaveAsync(student); // після вставки student.Id заповнено
                Students.Add(student);

                System.Diagnostics.Debug.WriteLine(
                    $"[StudentViewModel] Додано студента Id = {student.Id}");

                FullName = string.Empty;
                Group = string.Empty;
                AverageScore = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudentViewModel] Помилка додавання: {ex.Message}");
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

                await _repository.DeleteAsync(student);
                Students.Remove(student);

                System.Diagnostics.Debug.WriteLine(
                    $"[StudentViewModel] Видалено студента Id = {student.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudentViewModel] Помилка видалення: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
