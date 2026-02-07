using System;

namespace BuildBackup.Interfaces;

/// <summary>
/// Container for build state during a backup or analysis operation.
/// Holds parsed configuration files and provides per-operation isolation.
/// </summary>
public interface IBuildContext : IDisposable
{
    /// <summary>
    /// Product code (e.g., "wow_classic_era").
    /// </summary>
    string Product { get; }

    /// <summary>
    /// CDN path prefix (e.g., "tpr/wow").
    /// </summary>
    string CdnPath { get; set; }

    /// <summary>
    /// Parsed versions file.
    /// </summary>
    VersionsFile Versions { get; set; }

    /// <summary>
    /// Parsed CDNs file.
    /// </summary>
    CdnsFile Cdns { get; set; }

    /// <summary>
    /// Parsed build configuration.
    /// </summary>
    BuildConfigFile BuildConfig { get; set; }

    /// <summary>
    /// Parsed CDN configuration.
    /// </summary>
    CDNConfigFile CdnConfig { get; set; }

    /// <summary>
    /// Parsed product configuration.
    /// </summary>
    GameBlobFile ProductConfig { get; set; }

    /// <summary>
    /// Parsed encoding file.
    /// </summary>
    EncodingFile Encoding { get; set; }

    /// <summary>
    /// Parsed root file.
    /// </summary>
    RootFile Root { get; set; }

    /// <summary>
    /// Parsed install file.
    /// </summary>
    InstallFile Install { get; set; }

    /// <summary>
    /// Parsed download file.
    /// </summary>
    DownloadFile Download { get; set; }

    /// <summary>
    /// Parsed patch file.
    /// </summary>
    PatchFile Patch { get; set; }

    /// <summary>
    /// Thread-safe index cache for this build context.
    /// </summary>
    IIndexCache IndexCache { get; }
}
