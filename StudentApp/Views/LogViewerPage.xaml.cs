using StudentApp.ViewModels;

namespace StudentApp.Views;

public partial class LogViewerPage : ContentPage
{
    // ViewModel (разом із сервісом читання логів) підставляє DI-контейнер.
    public LogViewerPage(LogViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Щоразу при відкритті екрана перечитуємо свіжі записи.
        if (BindingContext is LogViewerViewModel viewModel)
            viewModel.LoadCommand.Execute(null);
    }
}
