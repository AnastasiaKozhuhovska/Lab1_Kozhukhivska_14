using StudentApp.Models;

namespace StudentApp.Data
{
    public interface IStudentRepository
    {
        /// <summary>Повертає всіх студентів із БД.</summary>
        Task<List<Student>> GetAllAsync();

        /// <summary>Повертає студента за Id або null, якщо такого немає.</summary>
        Task<Student?> GetByIdAsync(int id);

        /// <summary>Додає нового студента (Id == 0) або оновлює наявного. Повертає кількість змінених рядків.</summary>
        Task<int> SaveAsync(Student student);

        /// <summary>Видаляє студента. Повертає кількість видалених рядків.</summary>
        Task<int> DeleteAsync(Student student);
    }
}
