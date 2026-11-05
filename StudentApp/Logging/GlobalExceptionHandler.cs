using Serilog;

namespace StudentApp.Logging
{
    /// <summary>
    /// Глобальна обробка необроблених винятків: записує критичну помилку в лог
    /// (консоль + файл Serilog) до того, як процес завершить роботу.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static bool _registered;

        /// <summary>Підписується на глобальні події винятків. Викликається один раз у MauiProgram.</summary>
        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            // 1) Виняток, який ніхто не перехопив у жодному потоці → процес буде завершено.
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 2) Виняток у Task, результат якої ніхто не очікував (await) і не перевірив.
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

#if ANDROID
            // 3) Android: виняток, що дійшов до середовища Java/Android.
            global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
                LogFatal(e.Exception, "Android.UnhandledExceptionRaiser", isTerminating: true);
#endif

            Log.Debug("Глобальні обробники винятків зареєстровано");
        }

        /// <summary>
        /// Записує критичну помилку в лог. Якщо процес завершується — примусово
        /// дописує буфер Serilog у файл (Log.CloseAndFlush), щоб запис не загубився.
        /// Публічний, щоб платформені обробники (наприклад, WinUI) теж його використовували.
        /// </summary>
        public static void LogFatal(Exception? exception, string source, bool isTerminating)
        {
            try
            {
                Log.Fatal(exception,
                    "КРИТИЧНА ПОМИЛКА ({Source}). Тип: {ExceptionType}. Завершення роботи: {IsTerminating}",
                    source, exception?.GetType().FullName ?? "невідомо", isTerminating);

                if (isTerminating)
                {
                    Log.Information("Застосунок аварійно завершує роботу");
                    Log.CloseAndFlush();
                }
            }
            catch
            {
                // Обробник аварії сам ніколи не повинен кидати виняток.
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // ExceptionObject може бути не Exception (теоретично будь-який object).
            var exception = e.ExceptionObject as Exception
                            ?? new Exception($"Необроблений об'єкт винятку: {e.ExceptionObject}");

            LogFatal(exception, "AppDomain.UnhandledException", e.IsTerminating);
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // Записуємо всі внутрішні винятки задачі.
            foreach (var inner in e.Exception.Flatten().InnerExceptions)
            {
                Log.Error(inner, "Неспостережений виняток у фоновій задачі (TaskScheduler.UnobservedTaskException)");
            }

            // Позначаємо виняток як оброблений, щоб він не завершив процес.
            e.SetObserved();
        }
    }
}
