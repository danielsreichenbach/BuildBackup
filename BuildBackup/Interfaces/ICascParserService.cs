using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace BuildBackup.Interfaces;

/// <summary>
/// Service for parsing CASC (Content Addressable Storage Container) files.
/// </summary>
public interface ICascParserService
{
    /// <summary>
    /// Downloads and parses an encoding file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Encoding file hash (content hash or encoding key).</param>
    /// <param name="options">Parsing options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed encoding file.</returns>
    Task<EncodingFile> GetEncodingAsync(string cdnPath, string hash, EncodingParseOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses a root file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Root file hash.</param>
    /// <param name="parse">If true, parse the file contents. If false, just download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed root file.</returns>
    Task<RootFile> GetRootAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses an install file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Install file hash.</param>
    /// <param name="parse">If true, parse the file contents. If false, just download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed install file.</returns>
    Task<InstallFile> GetInstallAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses a download file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Download file hash.</param>
    /// <param name="parse">If true, parse the file contents. If false, just download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed download file.</returns>
    Task<DownloadFile> GetDownloadAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses a patch file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Patch file hash.</param>
    /// <param name="parse">If true, parse the file contents. If false, just download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed patch file.</returns>
    Task<PatchFile> GetPatchAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default);
}

/// <summary>
/// Options for encoding file parsing.
/// </summary>
public class EncodingParseOptions
{
    /// <summary>
    /// Expected file size for validation. 0 to skip validation.
    /// </summary>
    public int ExpectedSize { get; init; } = 0;

    /// <summary>
    /// If true, also parse Table B entries.
    /// </summary>
    public bool ParseTableB { get; init; } = false;

    /// <summary>
    /// If true, verify header checksums.
    /// </summary>
    public bool CheckHeaders { get; init; } = false;

    /// <summary>
    /// If true, file is BLTE-encoded. If false, read raw from disk.
    /// </summary>
    public bool IsEncoded { get; init; } = true;

    /// <summary>
    /// For non-encoded files, the local file path.
    /// </summary>
    public string? LocalPath { get; init; } = null;
}
