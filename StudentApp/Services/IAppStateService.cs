namespace StudentApp.Services
{

    public interface IAppStateService
    {
        string? CurrentRoute { get; set; }

        void SaveState();
        void RestoreState();
    }
}
