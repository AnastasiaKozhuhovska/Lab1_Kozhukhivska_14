using StudentApp.Models;

namespace StudentApp.Services.Sync
{
    /// <summary>Сервіс синхронізації офлайн-змін із сервером.</summary>
    public interface ISyncService
    {
        /// <summary>Чи є зараз доступ до інтернету.</summary>
        bool IsOnline { get; }

        /// <summary>Скільки змін чекає в черзі на відправлення.</summary>
        int PendingCount { get; }

        /// <summary>Подія зміни стану мережі або кількості змін у черзі.</summary>
        event EventHandler? StatusChanged;

        /// <summary>Запускає стеження за мережею і відправляє те, що лишилося в черзі.</summary>
        void Start();

        /// <summary>Записує CRUD-зміну в чергу; якщо мережа є, одразу запускає відправлення.</summary>
        Task EnqueueAsync(SyncOperation operation, Student student);

        /// <summary>Відправляє на сервер усі зміни з черги (у порядку додавання).</summary>
        Task ProcessQueueAsync();
    }
}
