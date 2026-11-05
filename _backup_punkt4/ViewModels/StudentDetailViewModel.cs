using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using StudentApp.Data;
using StudentApp.Models;

namespace StudentApp.ViewModels
{
    public class StudentDetailViewModel : IQueryAttributable, INotifyPropertyChanged
    {
        private readonly IStudentRepository _repository;

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
                    CreatedAt = student.CreatedAt
                };

                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Group));
                OnPropertyChanged(nameof(AverageScore));
                ((Command)SaveCommand).ChangeCanExecute();
            }
        }

        public ICommand GoBackCommand { get; }

        /// <summary>Зберігає зміни студента в БД через репозиторій.</summary>
        public ICommand SaveCommand { get; }

        public StudentDetailViewModel(IStudentRepository repository)
        {
            _repository = repository;

            GoBackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
            SaveCommand = new Command(async () => await SaveAsync(),
                                      () => !string.IsNullOrWhiteSpace(FullName));
        }

        private async Task SaveAsync()
        {
            try
            {
                // Id != 0 → UpdateAsync, Id == 0 (наприклад, студент із deep link) → InsertAsync.
                await _repository.SaveAsync(_student);

                System.Diagnostics.Debug.WriteLine(
                    $"[StudentDetailViewModel] Збережено студента Id = {_student.Id}");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StudentDetailViewModel] Помилка збереження: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
