using Microsoft.Maui.Graphics;

namespace StudentApp.Models
{
    /// <summary>Один запис лог-файлу Serilog, розібраний на частини.</summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; init; }

        /// <summary>Скорочений рівень: VRB, DBG, INF, WRN, ERR, FTL.</summary>
        public string Level { get; init; } = string.Empty;

        /// <summary>Клас-джерело (SourceContext), наприклад StudentApp.Services.Sync.SyncService.</summary>
        public string Source { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        /// <summary>Текст винятку та стек викликів (рядки після основного повідомлення).</summary>
        public string? Exception { get; set; }

        // ---- Властивості для відображення ----

        public string TimeText => Timestamp.ToString("dd.MM HH:mm:ss");

        public string ShortSource =>
            Source.Contains('.') ? Source[(Source.LastIndexOf('.') + 1)..] : Source;

        public bool HasException => !string.IsNullOrWhiteSpace(Exception);

        public Color LevelColor => Level switch
        {
            "VRB" or "DBG" => Colors.Gray,
            "INF" => Colors.SteelBlue,
            "WRN" => Colors.DarkOrange,
            "ERR" => Colors.OrangeRed,
            "FTL" => Colors.DarkRed,
            _ => Colors.Gray
        };
    }
}
