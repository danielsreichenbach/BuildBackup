using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildBackup.Interfaces;

#nullable enable

namespace BuildBackup.Services;

/// <summary>
/// CDN client for downloading files from Blizzard CDN servers with caching and failover.
/// </summary>
public class CdnClient : ICdnClient
{
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;
    private readonly List<string> _cdnServers;
    private readonly int _maxParallelDownloads;
    private SemaphoreSlim? _downloadSemaphore;
    private bool _disposed;

    public string CacheDir { get; }
    public string DecryptionKeyName { get; set; } = "";
    public IReadOnlyList<string> CdnServers => _cdnServers.AsReadOnly();

    /// <summary>
    /// Creates a new CDN client.
    /// </summary>
    /// <param name="httpClient">HTTP client for requests.</param>
    /// <param name="logger">Logger for errors.</param>
    /// <param name="cacheDir">Local cache directory.</param>
    /// <param name="initialCdnServers">Initial list of CDN server hostnames.</param>
    /// <param name="maxParallelDownloads">Maximum concurrent downloads.</param>
    public CdnClient(
        HttpClient httpClient,
        IAppLogger logger,
        string cacheDir,
        IEnumerable<string>? initialCdnServers = null,
        int maxParallelDownloads = 4)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CacheDir = cacheDir ?? throw new ArgumentNullException(nameof(cacheDir));
        _cdnServers = initialCdnServers != null ? new List<string>(initialCdnServers) : new List<string>();
        _maxParallelDownloads = Math.Max(1, maxParallelDownloads);
    }

    public void AddCdnServer(string hostname)
    {
        if (!string.IsNullOrEmpty(hostname) && !_cdnServers.Contains(hostname))
        {
            _cdnServers.Add(hostname);
        }
    }

    public void InitializeParallelDownloads()
    {
        if (_downloadSemaphore == null)
        {
            _downloadSemaphore = new SemaphoreSlim(_maxParallelDownloads, _maxParallelDownloads);
            Console.WriteLine($"[PARALLEL] Initialized with {_maxParallelDownloads} concurrent downloads");
        }
    }

    private void EnsureSemaphoreInitialized()
    {
        if (_downloadSemaphore == null)
        {
            Console.WriteLine($"[PARALLEL] WARNING: Semaphore was null, initializing on demand");
            _downloadSemaphore = new SemaphoreSlim(_maxParallelDownloads, _maxParallelDownloads);
        }
    }

    public async Task<uint> GetRemoteFileSizeAsync(string path, bool verbose = false)
    {
        path = path.ToLower();
        var found = false;

        foreach (var cdn in _cdnServers)
        {
            if (found) continue;

            var uri = new Uri("https://" + cdn + "/" + path);

            try
            {
                if (verbose)
                    Console.WriteLine($"[HTTP GET - SIZE CHECK] {uri.AbsoluteUri}");

                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    found = true;

                    if (response.Content.Headers.ContentLength != null)
                    {
                        var size = (uint)response.Content.Headers.ContentLength;
                        var humanSize = CdnUtils.FormatFileSize(size);
                        Console.WriteLine($"[SIZE] {path}: {humanSize}");
                        return size;
                    }
                    else
                    {
                        if (verbose)
                            Console.WriteLine($"[HTTP GET - SIZE FOUND] No content length header from {cdn}");
                        return 0;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogError("File not found on CDN " + cdn + " trying next CDN (if available)..");
                }
                else
                {
                    throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + uri.AbsoluteUri);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("!!! Error retrieving file size " + uri.AbsoluteUri + ": " + e.Message);
            }
        }

        if (!found)
        {
            if (verbose)
                Console.WriteLine($"[SIZE NOT FOUND] {path} - not found on any CDN");
            _logger.LogError("Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve filesize!", true);
            throw new FileNotFoundException($"File {path} could not be found on any CDN host for size check. This indicates the backup is incomplete.");
        }

        return 0;
    }

    public async Task<byte[]> GetAsync(string path, CdnGetOptions? options = null)
    {
        options ??= new CdnGetOptions();

        path = path.ToLower();
        var localPath = Path.Combine(CacheDir, path);

        // Debug: Log all file requests with full URL for better visibility
        var displayUrl = _cdnServers.Count > 0
            ? $"https://{_cdnServers[0]}/{path}"
            : path;
        Console.WriteLine($"[CDN GET] Requesting: {displayUrl}");

        var redownload = options.ForceRedownload;

        if (File.Exists(localPath) && options.ExpectedSize != 0)
        {
            var fileInfo = new FileInfo(localPath);
            if (fileInfo.Length != options.ExpectedSize)
            {
                if (options.Verbose)
                    Console.WriteLine("File size of " + path + " " + fileInfo.Length + " does not match expected size " + options.ExpectedSize + " redownloading..");

                redownload = true;
            }
        }

        var usingCache = !redownload && File.Exists(localPath);
        if (usingCache)
        {
            var cachedUrl = "https://" + (_cdnServers.Count > 0 ? _cdnServers[0] : "unknown") + "/" + path;
            Console.WriteLine($"[HTTP GET - CACHED] {cachedUrl} (skipping download)");
        }

        if (redownload || !File.Exists(localPath))
        {
            EnsureSemaphoreInitialized();

            await _downloadSemaphore!.WaitAsync();
            Console.WriteLine($"[PARALLEL] Acquired download slot for {path}");

            try
            {
                var found = false;

                foreach (var cdn in _cdnServers)
                {
                    if (found) continue;

                    var uri = new Uri("https://" + cdn + "/" + path);
                    var cleanName = uri.AbsolutePath;

                    if (options.Verbose)
                        Console.WriteLine("Downloading " + path + " from " + cdn);

                    try
                    {
                        var directoryPath = Path.GetDirectoryName(CacheDir + cleanName);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath!);
                        }

                        // Check if partial file exists for resume
                        var fullPath = CacheDir + cleanName;
                        var existingSize = 0L;
                        var canResume = false;

                        if (File.Exists(fullPath))
                        {
                            existingSize = new FileInfo(fullPath).Length;
                            if (existingSize > 0)
                            {
                                canResume = true;
                                Console.WriteLine($"[HTTP GET - RESUMING] Found partial file ({existingSize} bytes): {uri.AbsoluteUri}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[HTTP GET - DOWNLOADING] {uri.AbsoluteUri}");
                        }

                        // Prepare HTTP request with Range header if resuming
                        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        if (canResume)
                        {
                            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingSize, null);
                        }

                        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        using var responseStream = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode || (canResume && response.StatusCode == System.Net.HttpStatusCode.PartialContent))
                        {
                            // Use append mode if resuming, create new if starting fresh
                            using var file = canResume ? File.OpenWrite(fullPath) : File.Create(fullPath);
                            found = true;

                            // Seek to end if resuming
                            if (canResume)
                            {
                                file.Seek(0, SeekOrigin.End);
                            }

                            var buffer = new byte[4096];
                            int read;
                            long totalBytes = 0;
                            while ((read = await responseStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                file.Write(buffer, 0, read);
                                totalBytes += read;
                            }

                            if (canResume)
                            {
                                Console.WriteLine($"[HTTP GET - RESUMED] Downloaded {totalBytes} additional bytes (total: {existingSize + totalBytes}) from {cdn}");
                            }
                            else
                            {
                                Console.WriteLine($"[HTTP GET - SUCCESS] Downloaded {totalBytes} bytes from {cdn}");
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogError("File not found on CDN " + cdn + " trying next CDN (if available)..");
                        }
                        else
                        {
                            throw new FileNotFoundException("Error retrieving file: HTTP status code " +
                                                            response.StatusCode + " on URL " + uri.AbsoluteUri);
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        if (!e.CancellationToken.IsCancellationRequested)
                        {
                            _logger.LogError("!!! Timeout while retrieving file " + uri.AbsoluteUri);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("!!! Error retrieving file " + uri.AbsoluteUri + ": " + e.Message);
                    }
                }

                if (!found)
                {
                    Console.WriteLine($"[HTTP GET - FAILED] File {Path.GetFileNameWithoutExtension(path)} not found on any CDN");
                    _logger.LogError("Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve it!", true);
                    throw new FileNotFoundException($"File {path} could not be found on any CDN host. This indicates the backup is incomplete.");
                }
                else
                {
                    if (options.Verbose)
                        Console.WriteLine("Downloaded " + path);
                }
            }
            finally
            {
                _downloadSemaphore.Release();
                Console.WriteLine($"[PARALLEL] Released download slot for {path}");
            }
        }

        if (options.ReturnContent)
        {
            if (!string.IsNullOrEmpty(DecryptionKeyName))
                return BLTE.DecryptFile(Path.GetFileNameWithoutExtension(path), await File.ReadAllBytesAsync(localPath), DecryptionKeyName);
            else
                return await File.ReadAllBytesAsync(localPath);
        }
        else
        {
            return Array.Empty<byte>();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _downloadSemaphore?.Dispose();
        _disposed = true;
    }
}
