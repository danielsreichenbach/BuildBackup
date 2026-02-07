using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBackup.Interfaces;

/// <summary>
/// Service for parsing Blizzard configuration files.
/// </summary>
public interface IConfigParserService
{
    /// <summary>
    /// Gets version information for a product from Blizzard's API.
    /// </summary>
    /// <param name="product">Product code (e.g., "wow_classic_era").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed versions file.</returns>
    Task<VersionsFile> GetVersionsAsync(string product, CancellationToken ct = default);

    /// <summary>
    /// Gets CDN information for a product from Blizzard's API.
    /// </summary>
    /// <param name="product">Product code.</param>
    /// <param name="cdnHostsToPopulate">List to populate with discovered CDN hosts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed CDNs file.</returns>
    Task<CdnsFile> GetCdnsAsync(string product, IList<string> cdnHostsToPopulate, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses a build configuration file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix (e.g., "tpr/wow").</param>
    /// <param name="hash">Build config hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed build config.</returns>
    Task<BuildConfigFile> GetBuildConfigAsync(string cdnPath, string hash, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses a CDN configuration file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">CDN config hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed CDN config.</returns>
    Task<CDNConfigFile> GetCdnConfigAsync(string cdnPath, string hash, CancellationToken ct = default);

    /// <summary>
    /// Downloads and parses a product configuration file.
    /// </summary>
    /// <param name="cdnPath">CDN path prefix.</param>
    /// <param name="hash">Product config hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed product config.</returns>
    Task<GameBlobFile> GetProductConfigAsync(string cdnPath, string hash, CancellationToken ct = default);
}
