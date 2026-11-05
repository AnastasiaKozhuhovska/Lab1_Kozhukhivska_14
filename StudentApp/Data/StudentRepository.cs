using Microsoft.Extensions.Logging;
using SQLite;
using StudentApp.Models;
using StudentApp.Services.Security;
using StudentApp.Services.Sync;

namespace StudentApp.Data
{
    public class StudentRepository : IStudentRepository
    {
        private readonly ISyncService _syncService;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<StudentRepository> _logger;
        private SQLiteAsyncConnection? _database;

        public StudentRepository(ISyncService syncService, IEncryptionService encryption,
                                 ILogger<StudentRepository> logger)
        {
            _syncService = syncService;
            _encryption = encryption;
            _logger = logger;
        }

        // Ліниве підключення: БД і таблиця створюються при першому зверненні.
        // CreateTableAsync сам додає нову колонку StudentCardNumberEncrypted у вже наявну таблицю.
        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_database is not null)
                return _database;

            _database = new SQLiteAsyncConnection(DatabaseConstants.DatabasePath, DatabaseConstants.Flags);
            await _database.CreateTableAsync<Student>();

            _logger.LogInformation("Підключено БД: {DatabasePath}", DatabaseConstants.DatabasePath);

            return _database;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            try
            {
                var db = await GetConnectionAsync();
                var students = await db.Table<Student>()
                                       .OrderBy(s => s.FullName)
                                       .ToListAsync();

                foreach (var student in students)
                    await DecryptSensitiveFieldsAsync(student);

                _logger.LogInformation("З таблиці Students прочитано {Count} запис(ів)", students.Count);
                return students;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка читання списку студентів з БД");
                throw;
            }
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            try
            {
                var db = await GetConnectionAsync();
                var student = await db.Table<Student>()
                                      .Where(s => s.Id == id)
                                      .FirstOrDefaultAsync();

                if (student is null)
                {
                    _logger.LogWarning("Студента {StudentId} не знайдено в БД", id);
                    return null;
                }

                await DecryptSensitiveFieldsAsync(student);
                _logger.LogInformation("Прочитано студента {StudentId}", id);
                return student;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка читання студента {StudentId} з БД", id);
                throw;
            }
        }

        public async Task<int> SaveAsync(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            var isNew = student.Id == 0;

            try
            {
                var db = await GetConnectionAsync();

                // Шифруємо чутливе поле безпосередньо перед записом у БД.
                await EncryptSensitiveFieldsAsync(student);

                int rows;

                if (!isNew)
                {
                    rows = await db.UpdateAsync(student);

                    if (rows == 0)
                    {
                        _logger.LogWarning("UPDATE не змінив жодного рядка: студента {StudentId} немає в БД", student.Id);
                        return rows;
                    }

                    await _syncService.EnqueueAsync(SyncOperation.Update, student);
                }
                else
                {
                    student.CreatedAt = DateTime.Now;
                    rows = await db.InsertAsync(student); // після вставки student.Id заповнюється автоматично
                    await _syncService.EnqueueAsync(SyncOperation.Create, student);
                }

                // Номер студентського квитка в лог НЕ пишемо — це чутливі дані.
                _logger.LogInformation("Студента {StudentId} збережено в БД ({Operation})",
                    student.Id, isNew ? "INSERT" : "UPDATE");

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка збереження студента {StudentId} ({Operation})",
                    student.Id, isNew ? "INSERT" : "UPDATE");
                throw;
            }
        }

        public async Task<int> DeleteAsync(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            try
            {
                var db = await GetConnectionAsync();
                var rows = await db.DeleteAsync(student);

                if (rows == 0)
                {
                    _logger.LogWarning("DELETE не змінив жодного рядка: студента {StudentId} немає в БД", student.Id);
                    return rows;
                }

                await _syncService.EnqueueAsync(SyncOperation.Delete, student);

                _logger.LogInformation("Студента {StudentId} видалено з БД", student.Id);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка видалення студента {StudentId}", student.Id);
                throw;
            }
        }

        private async Task EncryptSensitiveFieldsAsync(Student student)
        {
            student.StudentCardNumberEncrypted = await _encryption.EncryptAsync(student.StudentCardNumber?.Trim());
        }

        private async Task DecryptSensitiveFieldsAsync(Student student)
        {
            student.StudentCardNumber = await _encryption.DecryptAsync(student.StudentCardNumberEncrypted);

            if (student.StudentCardNumber is null && !string.IsNullOrEmpty(student.StudentCardNumberEncrypted))
                _logger.LogWarning("Не вдалося розшифрувати номер квитка студента {StudentId}", student.Id);
        }
    }
}
