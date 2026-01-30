using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace BuildBackup.Interfaces;

/// <summary>
/// CDN client for downloading files from Blizzard CDN servers.
/// </summary>
public interface ICdnClient : IDisposable
{
    /// <summary>
    /// Local cache directory for downloaded files.
    /// </summary>
    string CacheDir { get; }

    /// <summary>
    /// Optional decryption key name for Armadillo-encrypted files.
    /// </summary>
    string DecryptionKeyName { get; set; }

    /// <summary>
    /// List of CDN server hostnames.
    /// </summary>
    IReadOnlyList<string> CdnServers { get; }

    /// <summary>
    /// Adds a CDN server to the list.
    /// </summary>
    /// <param name="hostname">CDN hostname without protocol.</param>
    void AddCdnServer(string hostname);

    /// <summary>
    /// Gets a file from the CDN, caching locally.
    /// </summary>
    /// <param name="path">Relative path on the CDN.</param>
    /// <param name="options">Optional download options.</param>
    /// <returns>File contents if returnContent is true, otherwise empty array.</returns>
    Task<byte[]> GetAsync(string path, CdnGetOptions? options = null);

    /// <summary>
    /// Gets the remote file size without downloading the full file.
    /// </summary>
    /// <param name="path">Relative path on the CDN.</param>
    /// <param name="verbose">Enable verbose logging.</param>
    /// <returns>File size in bytes, or 0 if unknown.</returns>
    Task<uint> GetRemoteFileSizeAsync(string path, bool verbose = false);

    /// <summary>
    /// Initializes the parallel download semaphore.
    /// </summary>
    void InitializeParallelDownloads();
}

/// <summary>
/// Options for CDN file retrieval.
/// </summary>
public class CdnGetOptions
{
    /// <summary>
    /// Whether to return file contents. Default true.
    /// </summary>
    public bool ReturnContent { get; init; } = true;

    /// <summary>
    /// Force re-download even if cached. Default false.
    /// </summary>
    public bool ForceRedownload { get; init; } = false;

    /// <summary>
    /// Expected file size for validation. 0 to skip validation.
    /// </summary>
    public uint ExpectedSize { get; init; } = 0;

    /// <summary>
    /// Enable verbose logging. Default false.
    /// </summary>
    public bool Verbose { get; init; } = false;
}

/// <summary>
/// Utility methods for CDN operations.
/// </summary>
public static class CdnUtils
{
    /// <summary>
    /// Formats a byte count as a human-readable string.
    /// </summary>
    public static string FormatFileSize(ulong bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {suffixes[suffixIndex]}";
    }
}
