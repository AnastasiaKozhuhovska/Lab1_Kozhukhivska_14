using SQLite;

namespace StudentApp.Models
{
    /// <summary>Тип зміни, яку потрібно відправити на сервер.</summary>
    public enum SyncOperation
    {
        Create = 0,
        Update = 1,
        Delete = 2
    }

    /// <summary>
    /// Один запис черги офлайн-змін. Зберігається в таблиці SyncQueue тієї ж БД SQLite,
    /// тому черга не губиться після закриття застосунку.
    /// </summary>
    [Table("SyncQueue")]
    public class SyncQueueItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>Тип сутності (наприклад, "Student").</summary>
        [NotNull, MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Id запису, якого стосується зміна.</summary>
        public int EntityId { get; set; }

        /// <summary>Операція: Create / Update / Delete.</summary>
        [NotNull]
        public SyncOperation Operation { get; set; }

        /// <summary>Дані запису у форматі JSON (для Delete — null).</summary>
        public string? Payload { get; set; }

        /// <summary>Коли зміну додано в чергу.</summary>
        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Кількість невдалих спроб відправлення.</summary>
        public int Attempts { get; set; }

        /// <summary>Текст останньої помилки відправлення.</summary>
        public string? LastError { get; set; }
    }
}
