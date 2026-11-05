using StudentApp.Models;

namespace StudentApp.Data
{
    /// <summary>Доступ до таблиці черги офлайн-змін SyncQueue.</summary>
    public interface ISyncQueueRepository
    {
        /// <summary>Додає зміну в кінець черги.</summary>
        Task EnqueueAsync(SyncQueueItem item);

        /// <summary>Повертає всі невідправлені зміни в порядку додавання (FIFO).</summary>
        Task<List<SyncQueueItem>> GetPendingAsync();

        /// <summary>Оновлює запис черги (кількість спроб, текст помилки).</summary>
        Task UpdateAsync(SyncQueueItem item);

        /// <summary>Видаляє зміну з черги після успішного відправлення.</summary>
        Task RemoveAsync(SyncQueueItem item);

        /// <summary>Кількість змін, що чекають на відправлення.</summary>
        Task<int> CountAsync();
    }
}
