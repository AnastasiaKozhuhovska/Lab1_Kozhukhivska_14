using StudentApp.ViewModels;

namespace StudentApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new StudentViewModel();
    }
}
