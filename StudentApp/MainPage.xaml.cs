using StudentApp.ViewModels;

namespace StudentApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new StudentViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is StudentViewModel viewModel &&
            viewModel.LoadStudentsCommand.CanExecute(null))
        {
            viewModel.LoadStudentsCommand.Execute(null);
        }

        System.Diagnostics.Debug.WriteLine("[MainPage] OnAppearing — сторінка списку студентів з'явилась.");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        System.Diagnostics.Debug.WriteLine("[MainPage] OnDisappearing — користувач покидає сторінку списку.");
    }
}
