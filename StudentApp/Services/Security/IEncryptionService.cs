namespace StudentApp.Services.Security
{
    /// <summary>Шифрування чутливих даних перед збереженням у БД.</summary>
    public interface IEncryptionService
    {
        /// <summary>Шифрує рядок. Повертає Base64(IV + шифротекст) або null для порожнього значення.</summary>
        Task<string?> EncryptAsync(string? plainText);

        /// <summary>Розшифровує рядок, отриманий з EncryptAsync. Повертає null, якщо розшифрувати не вдалося.</summary>
        Task<string?> DecryptAsync(string? cipherText);
    }
}
