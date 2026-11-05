using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using Serilog;
using StudentApp.Data;
using StudentApp.Logging;
using StudentApp.Services;
using StudentApp.Services.Api;
using StudentApp.Services.Logs;
using StudentApp.Services.Security;
using StudentApp.Services.Sync;
using StudentApp.ViewModels;
using StudentApp.Views;

namespace StudentApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Serilog налаштовується першим, щоб логувати весь запуск застосунку.
        Log.Logger = LoggingSetup.CreateLogger();
        GlobalExceptionHandler.Register(); // глобальні обробники необроблених винятків

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IAppStateService, AppStateService>();

        // Шифрування чутливих полів (ключ — у захищеному сховищі пристрою).
        builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);
        builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

        // Офлайн-синхронізація: стан мережі, черга змін, API-заглушка, сервіс синхронізації.
        builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
        builder.Services.AddSingleton<ISyncQueueRepository, SyncQueueRepository>();
        builder.Services.AddSingleton<IStudentApiClient, FakeStudentApiClient>();
        builder.Services.AddSingleton<ISyncService, SyncService>();

        // Репозиторій студентів (SQLite). Singleton — одне підключення до БД на весь застосунок.
        builder.Services.AddSingleton<IStudentRepository, StudentRepository>();

        // ViewModel-и та сторінки, у які DI підставляє репозиторій.
        builder.Services.AddTransient<StudentViewModel>();
        builder.Services.AddTransient<StudentDetailViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StudentDetailPage>();

        // Екран перегляду локальних логів.
        builder.Services.AddSingleton<ILogReaderService, LogReaderService>();
        builder.Services.AddTransient<LogViewerViewModel>();
        builder.Services.AddTransient<LogViewerPage>();

        // Замінюємо стандартні провайдери логування на Serilog:
        // тепер ILogger<T> у будь-якому класі пише в консоль і файл.
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        // Автоматичний збір крашів — Sentry (аналог App Center Crashes / Firebase Crashlytics).
        // Перехоплює необроблені винятки, зберігає звіт на диск і відправляє його на sentry.io
        // (звіт про фатальний краш відправляється при наступному запуску застосунку).
        builder.UseSentry(options =>
        {
            options.Dsn = CrashReportingSettings.SentryDsn;
#if DEBUG
            options.Environment = "debug";
#else
            options.Environment = "release";
#endif
            // Логи рівня Error і вище (ILogger) теж стають подіями в Sentry,
            // нижчі рівні додаються як "хлібні крихти" (breadcrumbs) до звіту.
            options.MinimumEventLevel = LogLevel.Error;
            options.MinimumBreadcrumbLevel = LogLevel.Information;

            // Персональні дані та скриншоти не надсилаємо.
            options.SendDefaultPii = false;
            options.AttachScreenshot = false;
        });

        var app = builder.Build();

        Log.Information("StudentApp запущено. Логи: {LogDirectory}", LoggingSetup.LogDirectory);

        if (CrashReportingSettings.IsConfigured)
            Log.Information("Sentry увімкнено: краші надсилаються на sentry.io");
        else
            Log.Warning("Sentry вимкнено: не вказано DSN у CrashReportingSettings.cs");

        return app;
    }
}