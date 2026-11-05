using StudentApp.ViewModels;

namespace StudentApp.Views;

public partial class StudentDetailPage : ContentPage
{
    // ViewModel (разом із репозиторієм) підставляє DI-контейнер.
    public StudentDetailPage(StudentDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
