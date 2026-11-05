using Microsoft.Maui.Storage;
using Serilog;
using Serilog.Events;

namespace StudentApp.Logging
{
    /// <summary>Налаштування Serilog: консоль (для дебагу) + файл із щоденною ротацією.</summary>
    public static class LoggingSetup
    {
        /// <summary>Скільки останніх файлів логів зберігати (старіші видаляються автоматично).</summary>
        private const int RetainedFileCountLimit = 7;

        /// <summary>Максимальний розмір одного файлу — 10 МБ.</summary>
        private const long FileSizeLimitBytes = 10 * 1024 * 1024;

        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        /// <summary>Папка з логами: AppDataDirectory/logs.</summary>
        public static string LogDirectory => Path.Combine(FileSystem.AppDataDirectory, "logs");

        public static Serilog.ILogger CreateLogger()
        {
            var configuration = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                // Службові повідомлення фреймворку — лише попередження та помилки.
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()

                // Sink 2: файл із щоденною ротацією та обмеженням кількості файлів.
                // Ім'я файлу: studentapp-20261101.log (дата додається автоматично).
                .WriteTo.File(
                    path: Path.Combine(LogDirectory, "studentapp-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: RetainedFileCountLimit,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    outputTemplate: OutputTemplate);

#if DEBUG
            // Sink 1: консоль — лише в Debug-збірці, для налагодження.
            configuration = configuration
                .WriteTo.Console(outputTemplate: OutputTemplate)
                // Додатково: вікно Output у Visual Studio (у MAUI на Windows консоль там не видно).
                .WriteTo.Debug(outputTemplate: OutputTemplate);
#endif

            return configuration.CreateLogger();
        }
    }
}
