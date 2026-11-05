using SQLite;
using StudentApp.Models;

namespace StudentApp.Data
{
    public class SyncQueueRepository : ISyncQueueRepository
    {
        private SQLiteAsyncConnection? _database;

        // Та сама БД, що й для студентів, але окрема таблиця SyncQueue.
        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_database is not null)
                return _database;

            _database = new SQLiteAsyncConnection(DatabaseConstants.DatabasePath, DatabaseConstants.Flags);
            await _database.CreateTableAsync<SyncQueueItem>();
            return _database;
        }

        public async Task EnqueueAsync(SyncQueueItem item)
        {
            var db = await GetConnectionAsync();
            await db.InsertAsync(item);
        }

        public async Task<List<SyncQueueItem>> GetPendingAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<SyncQueueItem>()
                           .OrderBy(i => i.Id)
                           .ToListAsync();
        }

        public async Task UpdateAsync(SyncQueueItem item)
        {
            var db = await GetConnectionAsync();
            await db.UpdateAsync(item);
        }

        public async Task RemoveAsync(SyncQueueItem item)
        {
            var db = await GetConnectionAsync();
            await db.DeleteAsync(item);
        }

        public async Task<int> CountAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<SyncQueueItem>().CountAsync();
        }
    }
}
