using System;
using BuildBackup.Interfaces;

namespace BuildBackup.Services;

/// <summary>
/// Container for build state during a backup or analysis operation.
/// Creates and owns an IndexCache instance for per-operation isolation.
/// </summary>
public sealed class BuildContext : IBuildContext
{
    private bool _disposed;

    /// <summary>
    /// Creates a new build context for the specified product.
    /// </summary>
    /// <param name="product">Product code (e.g., "wow_classic_era").</param>
    public BuildContext(string product)
    {
        Product = product ?? throw new ArgumentNullException(nameof(product));
        IndexCache = new IndexCache();
        CdnPath = string.Empty;
    }

    public string Product { get; }

    public string CdnPath { get; set; }

    public VersionsFile Versions { get; set; }

    public CdnsFile Cdns { get; set; }

    public BuildConfigFile BuildConfig { get; set; }

    public CDNConfigFile CdnConfig { get; set; }

    public GameBlobFile ProductConfig { get; set; }

    public EncodingFile Encoding { get; set; }

    public RootFile Root { get; set; }

    public InstallFile Install { get; set; }

    public DownloadFile Download { get; set; }

    public PatchFile Patch { get; set; }

    public IIndexCache IndexCache { get; }

    public void Dispose()
    {
        if (_disposed)
            return;

        IndexCache.Dispose();
        _disposed = true;
    }
}
