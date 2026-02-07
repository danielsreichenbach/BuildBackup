using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBackup.Interfaces;

/// <summary>
/// Service for parsing and managing archive index files.
/// </summary>
public interface IIndexService
{
    /// <summary>
    /// Downloads and parses a single archive index file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Archive hash.</param>
    /// <param name="folder">Folder type ("data" or "patch").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping encoding keys to index entries.</returns>
    Task<IReadOnlyDictionary<string, IndexEntry>> ParseIndexAsync(
        string cdnPath,
        string hash,
        string folder = "data",
        CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses multiple archive indexes in parallel, populating the cache.
    /// </summary>
    /// <param name="cache">Index cache to populate.</param>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="archives">Array of archive hashes.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PopulateIndexCacheAsync(
        IIndexCache cache,
        string cdnPath,
        string[] archives,
        CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses multiple patch archive indexes in parallel, populating the cache.
    /// </summary>
    /// <param name="cache">Index cache to populate.</param>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="archives">Array of patch archive hashes.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PopulatePatchIndexCacheAsync(
        IIndexCache cache,
        string cdnPath,
        string[] archives,
        CancellationToken ct = default);

    /// <summary>
    /// Parses a file index and returns the entries.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">File index hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping encoding keys to index entries.</returns>
    Task<IReadOnlyDictionary<string, IndexEntry>> ParseFileIndexAsync(
        string cdnPath,
        string hash,
        CancellationToken ct = default);
}
