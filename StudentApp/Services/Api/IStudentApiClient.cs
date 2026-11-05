using StudentApp.Models;

namespace StudentApp.Services.Api
{
    /// <summary>Клієнт серверного API для студентів.</summary>
    public interface IStudentApiClient
    {
        /// <summary>POST /api/students</summary>
        Task CreateAsync(Student student);

        /// <summary>PUT /api/students/{id}</summary>
        Task UpdateAsync(Student student);

        /// <summary>DELETE /api/students/{id}</summary>
        Task DeleteAsync(int id);
    }
}
