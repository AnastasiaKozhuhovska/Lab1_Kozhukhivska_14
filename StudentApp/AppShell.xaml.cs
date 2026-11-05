using StudentApp.Views;

namespace StudentApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("studentdetail", typeof(StudentDetailPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("logs", typeof(LogViewerPage));
    }
}
