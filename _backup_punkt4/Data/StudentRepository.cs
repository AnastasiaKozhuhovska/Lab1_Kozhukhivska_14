using SQLite;
using StudentApp.Models;

namespace StudentApp.Data
{
    public class StudentRepository : IStudentRepository
    {
        private SQLiteAsyncConnection? _database;

        // Ліниве підключення: БД і таблиця створюються при першому зверненні.
        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_database is not null)
                return _database;

            _database = new SQLiteAsyncConnection(DatabaseConstants.DatabasePath, DatabaseConstants.Flags);
            await _database.CreateTableAsync<Student>();

            System.Diagnostics.Debug.WriteLine(
                $"[StudentRepository] БД підключено: {DatabaseConstants.DatabasePath}");

            return _database;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            var db = await GetConnectionAsync();
            return await db.Table<Student>()
                           .OrderBy(s => s.FullName)
                           .ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            var db = await GetConnectionAsync();
            return await db.Table<Student>()
                           .Where(s => s.Id == id)
                           .FirstOrDefaultAsync();
        }

        public async Task<int> SaveAsync(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            var db = await GetConnectionAsync();

            if (student.Id != 0)
                return await db.UpdateAsync(student);

            student.CreatedAt = DateTime.Now;
            return await db.InsertAsync(student); // після вставки student.Id заповнюється автоматично
        }

        public async Task<int> DeleteAsync(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            var db = await GetConnectionAsync();
            return await db.DeleteAsync(student);
        }
    }
}
