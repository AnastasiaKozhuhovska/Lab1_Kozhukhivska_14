using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;
using StudentApp.Data;
using StudentApp.Models;
using StudentApp.Services.Api;
using StudentApp.Services.Security;

namespace StudentApp.Services.Sync
{
    public class SyncService : ISyncService
    {
        private readonly ISyncQueueRepository _queue;
        private readonly IStudentApiClient _api;
        private readonly IConnectivity _connectivity;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<SyncService> _logger;

        // Гарантує, що черга обробляється лише в одному потоці одночасно.
        private readonly SemaphoreSlim _processLock = new(1, 1);
        private bool _started;

        public bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

        public int PendingCount { get; private set; }

        public event EventHandler? StatusChanged;

        public SyncService(ISyncQueueRepository queue, IStudentApiClient api, IConnectivity connectivity,
                           IEncryptionService encryption, ILogger<SyncService> logger)
        {
            _queue = queue;
            _api = api;
            _connectivity = connectivity;
            _encryption = encryption;
            _logger = logger;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            // Реакція на появу / зникнення мережі.
            _connectivity.ConnectivityChanged += OnConnectivityChanged;

            _logger.LogInformation("Сервіс синхронізації запущено. Мережа: {NetworkAccess}", _connectivity.NetworkAccess);

            // Відправити зміни, що лишилися з попереднього запуску.
            _ = ProcessQueueAsync();
        }

        public async Task EnqueueAsync(SyncOperation operation, Student student)
        {
            var item = new SyncQueueItem
            {
                EntityType = nameof(Student),
                EntityId = student.Id,
                Operation = operation,
                // JSON містить номер студентського квитка у відкритому вигляді,
                // тому в таблицю SyncQueue він теж потрапляє лише зашифрованим.
                Payload = operation == SyncOperation.Delete
                    ? null
                    : await _encryption.EncryptAsync(JsonSerializer.Serialize(student)),
                CreatedAt = DateTime.Now
            };

            await _queue.EnqueueAsync(item);

            _logger.LogInformation("У чергу додано {Operation} для Student {StudentId}. Онлайн: {IsOnline}",
                operation, student.Id, IsOnline);

            await RefreshPendingCountAsync();

            // Якщо мережа є — відправляємо одразу, не блокуючи UI.
            if (IsOnline)
                _ = ProcessQueueAsync();
        }

        public async Task ProcessQueueAsync()
        {
            // Якщо обробка вже йде — повторно не запускаємо.
            if (!await _processLock.WaitAsync(0))
                return;

            try
            {
                await RefreshPendingCountAsync();

                if (!IsOnline && PendingCount > 0)
                    _logger.LogDebug("Мережі немає, у черзі чекає {PendingCount} змін(и)", PendingCount);

                while (IsOnline)
                {
                    var items = await _queue.GetPendingAsync();
                    if (items.Count == 0)
                        break;

                    _logger.LogInformation("Початок синхронізації: {Count} змін(и) у черзі", items.Count);

                    var failed = false;

                    foreach (var item in items)
                    {
                        if (!IsOnline)
                        {
                            failed = true;
                            break;
                        }

                        try
                        {
                            await SendAsync(item);
                            await _queue.RemoveAsync(item); // відправлено — прибираємо з черги

                            _logger.LogInformation("Синхронізовано {Operation} для Student {StudentId}",
                                item.Operation, item.EntityId);
                        }
                        catch (Exception ex)
                        {
                            // Зміна лишається в черзі; зупиняємось, щоб не порушити порядок операцій.
                            item.Attempts++;
                            item.LastError = ex.Message;
                            await _queue.UpdateAsync(item);

                            _logger.LogWarning(ex,
                                "Не вдалося відправити {Operation} для Student {StudentId} (спроба {Attempt})",
                                item.Operation, item.EntityId, item.Attempts);

                            failed = true;
                            break;
                        }
                        finally
                        {
                            await RefreshPendingCountAsync();
                        }
                    }

                    if (failed)
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка обробки черги синхронізації");
            }
            finally
            {
                _processLock.Release();
            }
        }

        private async Task SendAsync(SyncQueueItem item)
        {
            switch (item.Operation)
            {
                case SyncOperation.Create:
                    await _api.CreateAsync(await ReadPayloadAsync(item));
                    break;
                case SyncOperation.Update:
                    await _api.UpdateAsync(await ReadPayloadAsync(item));
                    break;
                case SyncOperation.Delete:
                    await _api.DeleteAsync(item.EntityId);
                    break;
                default:
                    throw new NotSupportedException($"Невідома операція: {item.Operation}");
            }
        }

        private async Task<Student> ReadPayloadAsync(SyncQueueItem item)
        {
            var json = await _encryption.DecryptAsync(item.Payload);

            // Записи, додані в чергу до появи шифрування, містять звичайний JSON.
            if (json is null && item.Payload?.TrimStart().StartsWith('{') == true)
                json = item.Payload;

            if (json is null)
                throw new InvalidOperationException("Не вдалося розшифрувати дані у записі черги.");

            return JsonSerializer.Deserialize<Student>(json)
                   ?? throw new InvalidOperationException("Порожні дані у записі черги.");
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            _logger.LogInformation("Зміна стану мережі: {NetworkAccess}", e.NetworkAccess);

            StatusChanged?.Invoke(this, EventArgs.Empty);

            // З'явився інтернет — відправляємо накопичені зміни.
            if (e.NetworkAccess == NetworkAccess.Internet)
                _ = ProcessQueueAsync();
        }

        private async Task RefreshPendingCountAsync()
        {
            PendingCount = await _queue.CountAsync();
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
