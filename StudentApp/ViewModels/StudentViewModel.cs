using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using StudentApp.Models;

namespace StudentApp.ViewModels
{
    public class StudentViewModel : IQueryAttributable, INotifyPropertyChanged
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

        public ObservableCollection<Student> Students { get; } = new();

        public ICommand AddStudentCommand { get; }

        public ICommand OpenDetailsCommand { get; }

        public ICommand LoadStudentsCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public StudentViewModel()
        {
            AddStudentCommand = new Command(AddStudent, CanAddStudent);
            OpenDetailsCommand = new Command<Student>(async (student) => await OpenDetailsAsync(student));
            LoadStudentsCommand = new Command(LoadStudents);
            OpenSettingsCommand = new Command(async () => await Shell.Current.GoToAsync("settings"));
        }

        private void LoadStudents()
        {
            System.Diagnostics.Debug.WriteLine(
                $"[StudentViewModel] LoadStudentsCommand виконано. У списку {Students.Count} студент(и/ів).");
        }

        private async Task OpenDetailsAsync(Student? student)
        {
            if (student is null) return;

            var parameters = new Dictionary<string, object>
            {
                { "SelectedStudent", student }
            };

            await Shell.Current.GoToAsync("studentdetail", parameters);
        }

        private void AddStudent()
        {
            Students.Add(new Student
            {
                FullName = FullName,
                Group = Group,
                AverageScore = AverageScore
            });


            FullName = string.Empty;
            Group = string.Empty;
            AverageScore = 0;
        }

        private bool CanAddStudent() => !string.IsNullOrWhiteSpace(FullName);


        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("UpdatedStudent", out var value) && value is Student updatedStudent)
            {
                var index = Students.IndexOf(updatedStudent);
                if (index >= 0)
                {
                    Students[index] = updatedStudent;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
