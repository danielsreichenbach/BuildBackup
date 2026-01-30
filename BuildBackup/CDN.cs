using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBackup
{
    public class CDN : IDisposable
    {
        private Services.CdnClient _cdnClient;
        private bool _disposed;
        

        // Backward-compatible public fields
        public HttpClient client;
        public string cacheDir;
        public List<string> cdnList;
        public string decryptionKeyName;

        /// <summary>
        /// Gets the underlying ICdnClient instance for dependency injection scenarios.
        /// Only available after InitializeParallelDownloads() is called.
        /// </summary>
        public Interfaces.ICdnClient Instance => _cdnClient;

        /// <summary>
        /// Creates a new CDN wrapper. Fields must be set before calling other methods.
        /// </summary>
        public CDN()
        {
            cdnList = new List<string>();
        }

        /// <summary>
        /// Creates a new CDN wrapper using the provided HttpClient and settings.
        /// </summary>
        public CDN(HttpClient httpClient, string cacheDir)
        {
            client = httpClient;
            this.cacheDir = cacheDir;
            cdnList = new List<string>();
        }

        private void EnsureInitialized()
        {
            if (_cdnClient == null)
            {
                _cdnClient = new Services.CdnClient(
                    client,
                    Logger.Instance,
                    cacheDir,
                    cdnList,
                    SettingsManager.maxParallelDownloads);
            }

            // Sync decryptionKeyName
            if (_cdnClient != null && decryptionKeyName != _cdnClient.DecryptionKeyName)
            {
                _cdnClient.DecryptionKeyName = decryptionKeyName ?? "";
            }

            // Sync any new CDN servers
            if (_cdnClient != null)
            {
                foreach (var cdn in cdnList)
                {
                    _cdnClient.AddCdnServer(cdn);
                }
            }
        }

        public void InitializeParallelDownloads()
        {
            EnsureInitialized();
            _cdnClient.InitializeParallelDownloads();
            
        }

        public static string FormatFileSize(ulong bytes)
        {
            return Interfaces.CdnUtils.FormatFileSize(bytes);
        }

        public async Task<uint> GetRemoteFileSize(string path, bool verbose = false)
        {
            EnsureInitialized();
            return await _cdnClient.GetRemoteFileSizeAsync(path, verbose);
        }

        public async Task<byte[]> Get(string path, bool returnstream = true, bool redownload = false, uint expectedSize = 0, bool verbose = false)
        {
            EnsureInitialized();

            var options = new Interfaces.CdnGetOptions
            {
                ReturnContent = returnstream,
                ForceRedownload = redownload,
                ExpectedSize = expectedSize,
                Verbose = verbose
            };

            return await _cdnClient.GetAsync(path, options);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cdnClient?.Dispose();
            _disposed = true;
        }
    }
}
