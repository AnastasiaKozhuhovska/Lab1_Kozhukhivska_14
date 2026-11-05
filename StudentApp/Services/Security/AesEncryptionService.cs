using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;

namespace StudentApp.Services.Security
{
    /// <summary>
    /// Шифрування AES-256 (режим CBC, PKCS7) з випадковим IV для кожного значення.
    /// Ключ генерується один раз і зберігається в SecureStorage
    /// (Android Keystore / iOS Keychain / Windows DPAPI), а не в БД і не в коді.
    /// </summary>
    public class AesEncryptionService : IEncryptionService
    {
        private const string KeyStorageName = "student_app_encryption_key";
        private const int KeySizeBytes = 32; // 256 біт
        private const int IvSizeBytes = 16;  // розмір блоку AES

        private readonly ISecureStorage _secureStorage;
        private readonly SemaphoreSlim _keyLock = new(1, 1);
        private byte[]? _key;

        public AesEncryptionService(ISecureStorage secureStorage)
        {
            _secureStorage = secureStorage;
        }

        public async Task<string?> EncryptAsync(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            using var aes = Aes.Create();
            aes.Key = await GetKeyAsync();

            // Новий IV для кожного шифрування: однакові номери дають різний шифротекст.
            var iv = RandomNumberGenerator.GetBytes(IvSizeBytes);
            var cipherBytes = aes.EncryptCbc(Encoding.UTF8.GetBytes(plainText), iv);

            // Зберігаємо IV разом із шифротекстом: [IV (16 байт)][шифротекст].
            var result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public async Task<string?> DecryptAsync(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return null;

            try
            {
                var data = Convert.FromBase64String(cipherText);
                if (data.Length <= IvSizeBytes)
                    return null;

                var iv = data.AsSpan(0, IvSizeBytes).ToArray();
                var cipherBytes = data.AsSpan(IvSizeBytes).ToArray();

                using var aes = Aes.Create();
                aes.Key = await GetKeyAsync();

                var plainBytes = aes.DecryptCbc(cipherBytes, iv);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                // Пошкоджені дані або інший ключ (наприклад, після перевстановлення застосунку).
                System.Diagnostics.Debug.WriteLine($"[AesEncryptionService] Не вдалося розшифрувати: {ex.Message}");
                return null;
            }
        }

        /// <summary>Повертає ключ із SecureStorage; при першому запуску генерує новий.</summary>
        private async Task<byte[]> GetKeyAsync()
        {
            if (_key is not null)
                return _key;

            await _keyLock.WaitAsync();
            try
            {
                if (_key is not null)
                    return _key;

                var stored = await ReadKeyAsync();
                if (!string.IsNullOrEmpty(stored))
                {
                    _key = Convert.FromBase64String(stored);
                }
                else
                {
                    _key = RandomNumberGenerator.GetBytes(KeySizeBytes);
                    await WriteKeyAsync(Convert.ToBase64String(_key));
                    System.Diagnostics.Debug.WriteLine("[AesEncryptionService] Згенеровано новий ключ шифрування.");
                }

                return _key;
            }
            finally
            {
                _keyLock.Release();
            }
        }

        private async Task<string?> ReadKeyAsync()
        {
            try
            {
                return await _secureStorage.GetAsync(KeyStorageName);
            }
            catch (Exception ex)
            {
                // Запасний варіант для платформ, де SecureStorage недоступне (лише для навчального проєкту).
                System.Diagnostics.Debug.WriteLine($"[AesEncryptionService] SecureStorage недоступне: {ex.Message}");
                return Preferences.Default.Get<string?>(KeyStorageName, null);
            }
        }

        private async Task WriteKeyAsync(string value)
        {
            try
            {
                await _secureStorage.SetAsync(KeyStorageName, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AesEncryptionService] SecureStorage недоступне: {ex.Message}");
                Preferences.Default.Set(KeyStorageName, value);
            }
        }
    }
}
