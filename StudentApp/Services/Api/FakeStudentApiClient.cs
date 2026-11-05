using Microsoft.Maui.Networking;
using StudentApp.Models;

namespace StudentApp.Services.Api
{
    /// <summary>
    /// Локальна заглушка серверного API. Імітує мережеву затримку, а без інтернету
    /// кидає HttpRequestException, як справжній HttpClient.
    /// "Серверні" дані зберігаються в пам'яті, результат видно у вікні Output (Debug).
    /// </summary>
    public class FakeStudentApiClient : IStudentApiClient
    {
        private static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(500);

        private readonly IConnectivity _connectivity;
        private readonly Dictionary<int, Student> _serverStorage = new();

        public FakeStudentApiClient(IConnectivity connectivity)
        {
            _connectivity = connectivity;
        }

        public async Task CreateAsync(Student student)
        {
            await SimulateRequestAsync("POST", "/api/students");
            _serverStorage[student.Id] = student;
            LogServerState();
        }

        public async Task UpdateAsync(Student student)
        {
            await SimulateRequestAsync("PUT", $"/api/students/{student.Id}");
            _serverStorage[student.Id] = student;
            LogServerState();
        }

        public async Task DeleteAsync(int id)
        {
            await SimulateRequestAsync("DELETE", $"/api/students/{id}");
            _serverStorage.Remove(id);
            LogServerState();
        }

        private async Task SimulateRequestAsync(string method, string url)
        {
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                throw new HttpRequestException("Немає з'єднання з мережею.");

            await Task.Delay(Latency); // імітація часу відповіді сервера

            System.Diagnostics.Debug.WriteLine($"[FakeStudentApi] {method} {url} → 200 OK");
        }

        private void LogServerState() =>
            System.Diagnostics.Debug.WriteLine(
                $"[FakeStudentApi] На сервері зараз {_serverStorage.Count} студент(и/ів).");
    }
}
