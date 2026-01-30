using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BuildBackup.Interfaces;


#nullable enable

namespace BuildBackup.Services;

/// <summary>
/// Service for managing TACT encryption keys with async loading.
/// </summary>
public class TactKeyService : IKeyService
{
    private const string DefaultKeysUrl = "https://github.com/wowdev/TACTKeys/raw/refs/heads/master/WoW.txt";
    private const string DefaultLocalFile = "tactkeys.txt";
    private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly Dictionary<ulong, byte[]> _keys = new();
    private readonly Salsa20 _salsa = new();
    private readonly string _cacheFilePath;
    private readonly TimeSpan _cacheExpiry;
    private readonly string _keysUrl;
    private bool _keysLoaded;

    public Salsa20 SalsaInstance => _salsa;

    /// <summary>
    /// Creates a new TactKeyService.
    /// </summary>
    /// <param name="httpClient">HTTP client for downloading keys.</param>
    /// <param name="cacheFilePath">Local cache file path.</param>
    /// <param name="cacheExpiry">Cache expiry duration.</param>
    /// <param name="keysUrl">URL to download keys from.</param>
    public TactKeyService(
        HttpClient httpClient,
        string? cacheFilePath = null,
        TimeSpan? cacheExpiry = null,
        string? keysUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cacheFilePath = cacheFilePath ?? DefaultLocalFile;
        _cacheExpiry = cacheExpiry ?? DefaultCacheExpiry;
        _keysUrl = keysUrl ?? DefaultKeysUrl;
    }

    public byte[]? GetKey(ulong keyName)
    {
        return _keys.TryGetValue(keyName, out var key) ? key : null;
    }

    public bool IsKeyAvailable(ulong keyName)
    {
        return _keys.ContainsKey(keyName);
    }

    public async Task LoadKeysAsync()
    {
        if (_keysLoaded)
            return;

        string? content = null;

        // Check if local cache exists and is fresh
        if (File.Exists(_cacheFilePath))
        {
            var fileAge = DateTime.Now - File.GetLastWriteTime(_cacheFilePath);
            if (fileAge < _cacheExpiry)
            {
                content = await File.ReadAllTextAsync(_cacheFilePath);
            }
        }

        // Download if no valid cache
        if (string.IsNullOrEmpty(content))
        {
            content = await DownloadKeysAsync();
        }

        // Parse keys if we have content
        if (!string.IsNullOrEmpty(content))
        {
            ParseKeys(content);
        }

        _keysLoaded = true;
    }

    /// <summary>
    /// Synchronously loads keys. Use LoadKeysAsync for non-blocking operation.
    /// </summary>
    public void LoadKeys()
    {
        LoadKeysAsync().GetAwaiter().GetResult();
    }

    private async Task<string?> DownloadKeysAsync()
    {
        try
        {
            Console.WriteLine($"[HTTP GET] {_keysUrl}");
            var response = await _httpClient.GetAsync(_keysUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to download TACT keys: HTTP {(int)response.StatusCode}");
                return await TryFallbackToLocalCacheAsync();
            }

            var content = await response.Content.ReadAsStringAsync();

            // Cache locally
            await File.WriteAllTextAsync(_cacheFilePath, content);
            Console.WriteLine($"Downloaded TACT keys, cached to {_cacheFilePath}");

            return content;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to download TACT keys: {e.Message}");
            return await TryFallbackToLocalCacheAsync();
        }
    }

    private async Task<string?> TryFallbackToLocalCacheAsync()
    {
        if (File.Exists(_cacheFilePath))
        {
            Console.WriteLine($"Using cached keys from {_cacheFilePath}");
            return await File.ReadAllTextAsync(_cacheFilePath);
        }

        return null;
    }

    private void ParseKeys(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var splitLine = line.Split(' ');
            if (splitLine.Length < 2)
                continue;

            try
            {
                var lookup = ulong.Parse(splitLine[0], System.Globalization.NumberStyles.HexNumber);
                byte[] key = splitLine[1].Trim().ToByteArray();

                if (!_keys.ContainsKey(lookup))
                {
                    _keys.Add(lookup, key);
                }
            }
            catch
            {
                // Skip malformed lines
            }
        }

        Console.WriteLine($"Loaded {_keys.Count} TACT keys");
    }
}
