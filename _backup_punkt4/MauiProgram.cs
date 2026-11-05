using Microsoft.Extensions.Logging;
using StudentApp.Data;
using StudentApp.Services;
using StudentApp.ViewModels;
using StudentApp.Views;

namespace StudentApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IAppStateService, AppStateService>();

        // Репозиторій студентів (SQLite). Singleton — одне підключення до БД на весь застосунок.
        builder.Services.AddSingleton<IStudentRepository, StudentRepository>();

        // ViewModel-и та сторінки, у які DI підставляє репозиторій.
        builder.Services.AddTransient<StudentViewModel>();
        builder.Services.AddTransient<StudentDetailViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StudentDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}