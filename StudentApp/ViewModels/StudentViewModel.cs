using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using StudentApp.Models;

namespace StudentApp.ViewModels
{
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly Student _student = new();

        public string FullName
        {
            get => _student.FullName;
            set
            {
                if (_student.FullName != value)
                {
                    _student.FullName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GreetingText));

                    // ПІБ впливає на CanAddStudent — примушуємо кнопку
                    // перевірити свій стан IsEnabled заново
                    ((Command)AddStudentCommand).ChangeCanExecute();
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
                    OnPropertyChanged(nameof(GreetingText));
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
                    OnPropertyChanged(nameof(AverageScoreColor));
                }
            }
        }

        public string GreetingText => $"Студент: {FullName}, група {Group}";

        public Color AverageScoreColor =>
            AverageScore >= 4.0 ? Colors.SeaGreen : Colors.OrangeRed;

        // Колекція доданих студентів — CollectionView у XAML прив'язується саме до неї
        public ObservableCollection<Student> Students { get; } = new();

        // Команда додавання поточного студента до списку
        public ICommand AddStudentCommand { get; }

        public StudentViewModel()
        {
            AddStudentCommand = new Command(AddStudent, CanAddStudent);
        }

        private void AddStudent()
        {
            Students.Add(new Student
            {
                FullName = FullName,
                Group = Group,
                AverageScore = AverageScore
            });

            // Очищаємо поля вводу після додавання
            FullName = string.Empty;
            Group = string.Empty;
            AverageScore = 0;
        }

        // Кнопка активна, лише якщо ПІБ не порожнє
        private bool CanAddStudent() => !string.IsNullOrWhiteSpace(FullName);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
