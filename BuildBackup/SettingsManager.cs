using System.Linq;
using Microsoft.Extensions.Configuration;

namespace BuildBackup
{
    public static class SettingsManager
    {
        private static readonly Interfaces.ISettings _settings;

        // Backward-compatible public fields that delegate to the underlying ISettings instance
        public static string cacheDir => _settings.CacheDir;
        public static string[] checkProducts => _settings.CheckProducts.ToArray();
        public static string[] backupProducts => _settings.BackupProducts.ToArray();
        public static bool downloadPatchFiles => _settings.DownloadPatchFiles;
        public static int maxParallelDownloads => _settings.MaxParallelDownloads;

        /// <summary>
        /// Gets the underlying ISettings instance for dependency injection scenarios.
        /// </summary>
        public static Interfaces.ISettings Instance => _settings;

        static SettingsManager()
        {
            _settings = Services.AppSettings.LoadFromFile();
        }

        /// <summary>
        /// Reloads settings from config.json. Primarily for testing.
        /// </summary>
        public static void LoadSettings()
        {
            // For backward compatibility, this method exists but the static constructor
            // handles initial loading. Calling this after startup has no effect since
            // the underlying ISettings is immutable.
        }
    }
}
