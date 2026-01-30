using System.Collections.Generic;

namespace BuildBackup.Interfaces;

/// <summary>
/// Provides read-only access to application configuration settings.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Local directory for caching downloaded files.
    /// </summary>
    string CacheDir { get; }

    /// <summary>
    /// Products to monitor for updates.
    /// </summary>
    IReadOnlyList<string> CheckProducts { get; }

    /// <summary>
    /// Products to fully backup.
    /// </summary>
    IReadOnlyList<string> BackupProducts { get; }

    /// <summary>
    /// Whether to download patch files.
    /// </summary>
    bool DownloadPatchFiles { get; }

    /// <summary>
    /// Maximum concurrent downloads (minimum 1).
    /// </summary>
    int MaxParallelDownloads { get; }
}
