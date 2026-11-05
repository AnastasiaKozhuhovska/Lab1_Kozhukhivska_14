using System.Text.Json.Serialization;
using SQLite;

namespace StudentApp.Models
{
    [Table("Students")]
    public class Student
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        // "GROUP" — зарезервоване слово SQL, тому колонка в БД називається GroupName.
        [NotNull, MaxLength(20), Column("GroupName")]
        public string Group { get; set; } = string.Empty;

        [NotNull]
        public double AverageScore { get; set; }

        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ---- Чутливе поле: номер студентського квитка ----

        /// <summary>
        /// Номер студентського квитка у відкритому вигляді.
        /// [Ignore] — НЕ зберігається в БД, існує лише в пам'яті застосунку.
        /// </summary>
        [Ignore]
        public string? StudentCardNumber { get; set; }

        /// <summary>
        /// Зашифрований номер (Base64 від IV + шифротексту AES-256). Саме це значення лежить у БД.
        /// Заповнює і читає StudentRepository через IEncryptionService.
        /// </summary>
        [Column("StudentCardNumberEncrypted"), JsonIgnore]
        public string? StudentCardNumberEncrypted { get; set; }

        /// <summary>Маскований номер для показу в списку: ****1234.</summary>
        [Ignore, JsonIgnore]
        public string MaskedCardNumber =>
            string.IsNullOrEmpty(StudentCardNumber)
                ? "квиток: —"
                : "квиток: ****" + StudentCardNumber[^Math.Min(4, StudentCardNumber.Length)..];
    }
}
