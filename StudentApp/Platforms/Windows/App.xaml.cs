using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StudentApp.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // WinUI перехоплює винятки UI-потоку раніше за AppDomain.UnhandledException,
            // тому підписуємось і тут, щоб критична помилка точно потрапила в лог.
            this.UnhandledException += OnWinUIUnhandledException;
        }

        private void OnWinUIUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            StudentApp.Logging.GlobalExceptionHandler.LogFatal(
                e.Exception, "WinUI.Application.UnhandledException", isTerminating: !e.Handled);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
