using StudentApp.ViewModels;

namespace StudentApp;

public partial class MainPage : ContentPage
{
    // ViewModel (разом із репозиторієм) підставляє DI-контейнер.
    public MainPage(StudentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Щоразу, коли сторінка з'являється (зокрема після редагування), перечитуємо список із БД.
        if (BindingContext is StudentViewModel viewModel &&
            viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }

        System.Diagnostics.Debug.WriteLine("[MainPage] OnAppearing — сторінка списку студентів з'явилась.");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        System.Diagnostics.Debug.WriteLine("[MainPage] OnDisappearing — користувач покидає сторінку списку.");
    }
}
