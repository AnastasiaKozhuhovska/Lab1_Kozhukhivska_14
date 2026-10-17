using StudentApp.ViewModels;

namespace StudentApp.Views;

public partial class StudentDetailPage : ContentPage
{
    public StudentDetailPage()
    {
        InitializeComponent();
        BindingContext = new StudentDetailViewModel();
    }
}
