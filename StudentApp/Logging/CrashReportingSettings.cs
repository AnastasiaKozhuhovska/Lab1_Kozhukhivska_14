namespace StudentApp.Logging
{
    /// <summary>
    /// Налаштування сервісу автоматичного збору крашів Sentry (sentry.io).
    /// </summary>
    public static class CrashReportingSettings
    {
        /// <summary>
        /// DSN проєкту Sentry: sentry.io → Settings → Projects → (проєкт) → Client Keys (DSN).
        /// Має вигляд https://xxxxxxxx@o000000.ingest.us.sentry.io/0000000
        /// Якщо рядок порожній — Sentry вимкнено, застосунок працює як раніше.
        /// </summary>
        public const string SentryDsn = "";

        public static bool IsConfigured => !string.IsNullOrWhiteSpace(SentryDsn);
    }
}
