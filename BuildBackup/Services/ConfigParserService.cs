using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuildBackup.Interfaces;

namespace BuildBackup.Services;

/// <summary>
/// Service for parsing Blizzard configuration files.
/// Handles versions, CDNs, build config, CDN config, and product config files.
/// </summary>
public sealed class ConfigParserService : IConfigParserService
{
    private static readonly Uri BaseUrl = new Uri("https://us.version.battle.net/");

    private readonly HttpClient _httpClient;
    private readonly ICdnClient _cdnClient;

    /// <summary>
    /// Creates a new config parser service.
    /// </summary>
    /// <param name="httpClient">HTTP client for API requests.</param>
    /// <param name="cdnClient">CDN client for downloading config files.</param>
    public ConfigParserService(HttpClient httpClient, ICdnClient cdnClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cdnClient = cdnClient ?? throw new ArgumentNullException(nameof(cdnClient));
    }

    public async Task<VersionsFile> GetVersionsAsync(string product, CancellationToken ct = default)
    {
        var versions = new VersionsFile();

        var versionsUrl = new Uri(BaseUrl, $"v2/products/{product}/versions");
        Console.WriteLine($"[HTTP GET] {versionsUrl.AbsoluteUri}");

        using var response = await _httpClient.GetAsync(versionsUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error during retrieving HTTP versions: Received bad HTTP code {response.StatusCode}");
            return versions;
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        content = content.Replace("\0", "");

        var lines = content.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Length > 0 && line[0] != '#')
            .ToArray();

        if (lines.Length == 0)
            return versions;

        versions.entries = new VersionsEntry[lines.Length - 1];
        var cols = lines[0].Split('|');

        for (var c = 0; c < cols.Length; c++)
        {
            var friendlyName = cols[c].Split('!')[0];

            for (var i = 1; i < lines.Length; i++)
            {
                var row = lines[i].Split('|');

                switch (friendlyName)
                {
                    case "Region":
                        versions.entries[i - 1].region = row[c];
                        break;
                    case "BuildConfig":
                        versions.entries[i - 1].buildConfig = row[c];
                        break;
                    case "CDNConfig":
                        versions.entries[i - 1].cdnConfig = row[c];
                        break;
                    case "Keyring":
                    case "KeyRing":
                        versions.entries[i - 1].keyRing = row[c];
                        break;
                    case "BuildId":
                        versions.entries[i - 1].buildId = row[c];
                        break;
                    case "VersionName":
                    case "VersionsName":
                        versions.entries[i - 1].versionsName = row[c].Trim('\r');
                        break;
                    case "ProductConfig":
                        versions.entries[i - 1].productConfig = row[c];
                        break;
                    default:
                        Console.WriteLine($"!!!!!!!! Unknown versions variable '{friendlyName}'");
                        break;
                }
            }
        }

        return versions;
    }

    public async Task<CdnsFile> GetCdnsAsync(string product, IList<string> cdnHostsToPopulate, CancellationToken ct = default)
    {
        var cdns = new CdnsFile();

        // Special case: gryphon uses hardcoded CDNs
        if (product == "gryphon")
        {
            cdns.entries = new CdnsEntry[1];
            cdns.entries[0].hosts = new[] { "cdn.blizzard.com", "blzddist1-a.akamaihd.net" };
            cdns.entries[0].path = "tpr/gryphon";
            cdns.entries[0].configPath = "configs/data/";
            return cdns;
        }

        var cdnsUrl = new Uri(BaseUrl, $"v2/products/{product}/cdns");
        Console.WriteLine($"[HTTP GET] {cdnsUrl.AbsoluteUri}");

        using var response = await _httpClient.GetAsync(cdnsUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error during retrieving HTTP cdns: Received bad HTTP code {response.StatusCode}");
            return cdns;
        }

        var content = await response.Content.ReadAsStringAsync(ct);

        var lines = content.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Length > 0 && line[0] != '#')
            .ToArray();

        if (lines.Length == 0)
            return cdns;

        cdns.entries = new CdnsEntry[lines.Length - 1];
        var cols = lines[0].Split('|');

        for (var c = 0; c < cols.Length; c++)
        {
            var friendlyName = cols[c].Split('!')[0];

            for (var i = 1; i < lines.Length; i++)
            {
                var row = lines[i].Split('|');

                switch (friendlyName)
                {
                    case "Name":
                        cdns.entries[i - 1].name = row[c];
                        break;
                    case "Path":
                        cdns.entries[i - 1].path = row[c];
                        break;
                    case "Hosts":
                        var hosts = row[c].Split(' ');
                        cdns.entries[i - 1].hosts = new string[hosts.Length];
                        for (var h = 0; h < hosts.Length; h++)
                        {
                            cdns.entries[i - 1].hosts[h] = hosts[h];
                        }
                        break;
                    case "ConfigPath":
                        cdns.entries[i - 1].configPath = row[c];
                        break;
                }
            }
        }

        // Populate CDN hosts list
        foreach (var subcdn in cdns.entries)
        {
            foreach (var cdnHost in subcdn.hosts)
            {
                if (!cdnHostsToPopulate.Contains(cdnHost))
                {
                    Console.WriteLine($"[CDN LIST] Adding new CDN from response: {cdnHost}");
                    cdnHostsToPopulate.Add(cdnHost);
                }
            }
        }

        Console.WriteLine($"[CDN LIST] Final CDN list ({cdnHostsToPopulate.Count} servers):");
        foreach (var cdnServer in cdnHostsToPopulate)
        {
            Console.WriteLine($"  - {cdnServer}");
        }

        return cdns;
    }

    public async Task<BuildConfigFile> GetBuildConfigAsync(string cdnPath, string hash, CancellationToken ct = default)
    {
        var buildConfig = new BuildConfigFile();

        string content;
        try
        {
            var path = CombinePath(cdnPath, $"{hash[..2]}/{hash[2..4]}/{hash}");
            var data = await _cdnClient.GetAsync(path);
            content = Encoding.UTF8.GetString(data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving build config: {e.Message}");
            return buildConfig;
        }

        if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
        {
            Console.WriteLine("Error reading build config");
            return buildConfig;
        }

        var lines = content.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("#") || line.Length == 0)
                continue;

            var cols = line.Split(new[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
            if (cols.Length < 2)
                continue;

            switch (cols[0])
            {
                case "root":
                    buildConfig.root = cols[1];
                    break;
                case "download":
                    buildConfig.download = cols[1].Split(' ');
                    break;
                case "install":
                    buildConfig.install = cols[1].Split(' ');
                    break;
                case "encoding":
                    buildConfig.encoding = cols[1].Split(' ');
                    break;
                case "encoding-size":
                    buildConfig.encodingSize = cols[1].Split(' ');
                    break;
                case "size":
                    buildConfig.size = cols[1].Split(' ');
                    break;
                case "size-size":
                    buildConfig.sizeSize = cols[1].Split(' ');
                    break;
                case "build-name":
                    buildConfig.buildName = cols[1];
                    break;
                case "build-playbuild-installer":
                    buildConfig.buildPlaybuildInstaller = cols[1];
                    break;
                case "build-product":
                    buildConfig.buildProduct = cols[1];
                    break;
                case "build-uid":
                    buildConfig.buildUid = cols[1];
                    break;
                case "patch":
                    buildConfig.patch = cols[1];
                    break;
                case "patch-size":
                    buildConfig.patchSize = cols[1];
                    break;
                case "patch-config":
                    buildConfig.patchConfig = cols[1];
                    break;
                case "build-branch":
                    buildConfig.buildBranch = cols[1];
                    break;
                case "build-num":
                case "build-number":
                case "build-version":
                    buildConfig.buildNumber = cols[1];
                    break;
                case "build-attributes":
                    buildConfig.buildAttributes = cols[1];
                    break;
                case "build-comments":
                    buildConfig.buildComments = cols[1];
                    break;
                case "build-creator":
                    buildConfig.buildCreator = cols[1];
                    break;
                case "build-fixed-hash":
                    buildConfig.buildFixedHash = cols[1];
                    break;
                case "build-replay-hash":
                    buildConfig.buildReplayHash = cols[1];
                    break;
                case "build-t1-manifest-version":
                    buildConfig.buildManifestVersion = cols[1];
                    break;
                case "install-size":
                    buildConfig.installSize = cols[1].Split(' ');
                    break;
                case "download-size":
                    buildConfig.downloadSize = cols[1].Split(' ');
                    break;
                case "build-partial-priority":
                case "partial-priority":
                    buildConfig.partialPriority = cols[1];
                    break;
                case "partial-priority-size":
                    buildConfig.partialPrioritySize = cols[1];
                    break;
                case "build-signature-file":
                    buildConfig.buildSignatureFile = cols[1];
                    break;
                case "patch-index":
                    buildConfig.patchIndex = cols[1].Split(' ');
                    break;
                case "patch-index-size":
                    buildConfig.patchIndexSize = cols[1].Split(' ');
                    break;
            }
        }

        return buildConfig;
    }

    public async Task<CDNConfigFile> GetCdnConfigAsync(string cdnPath, string hash, CancellationToken ct = default)
    {
        var cdnConfig = new CDNConfigFile();

        string content;
        try
        {
            var path = CombinePath(cdnPath, $"{hash[..2]}/{hash[2..4]}/{hash}");
            var data = await _cdnClient.GetAsync(path);
            content = Encoding.UTF8.GetString(data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving CDN config: {e.Message}");
            return cdnConfig;
        }

        var lines = content.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.StartsWith("#") || line.Length == 0)
                continue;

            var cols = line.Split(new[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
            if (cols.Length < 2)
                continue;

            switch (cols[0])
            {
                case "archives":
                    cdnConfig.archives = cols[1].Split(' ');
                    break;
                case "archive-group":
                    cdnConfig.archiveGroup = cols[1];
                    break;
                case "patch-archives":
                    cdnConfig.patchArchives = cols[1].Split(' ');
                    break;
                case "patch-archive-group":
                    cdnConfig.patchArchiveGroup = cols[1];
                    break;
                case "builds":
                    cdnConfig.builds = cols[1].Split(' ');
                    break;
                case "file-index":
                    cdnConfig.fileIndex = cols[1];
                    break;
                case "file-index-size":
                    cdnConfig.fileIndexSize = cols[1];
                    break;
                case "patch-file-index":
                    cdnConfig.patchFileIndex = cols[1];
                    break;
                case "patch-file-index-size":
                    cdnConfig.patchFileIndexSize = cols[1];
                    break;
            }
        }

        return cdnConfig;
    }

    public async Task<GameBlobFile> GetProductConfigAsync(string cdnPath, string hash, CancellationToken ct = default)
    {
        var gblob = new GameBlobFile();

        string content;
        try
        {
            var path = CombinePath(cdnPath, $"{hash[..2]}/{hash[2..4]}/{hash}");
            var data = await _cdnClient.GetAsync(path);
            content = Encoding.UTF8.GetString(data);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving product config: {e.Message}");
            return gblob;
        }

        if (string.IsNullOrEmpty(content))
        {
            Console.WriteLine("Error reading product config!");
            return gblob;
        }

        dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content)!;
        if (json.all?.config?.decryption_key_name != null)
        {
            gblob.decryptionKeyName = json.all.config.decryption_key_name.Value;
        }

        return gblob;
    }

    private static string CombinePath(string basePath, string relativePath)
    {
        if (basePath.EndsWith("/"))
            basePath = basePath[..^1];
        if (relativePath.StartsWith("/"))
            relativePath = relativePath[1..];
        return $"{basePath}/{relativePath}";
    }
}
