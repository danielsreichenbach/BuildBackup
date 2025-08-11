using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBackup
{
    public class CDN
    {
        public HttpClient client;
        public string cacheDir;
        public List<string> cdnList;
        private static SemaphoreSlim downloadSemaphore;

        public void InitializeParallelDownloads()
        {
            if (downloadSemaphore == null)
            {
                downloadSemaphore = new SemaphoreSlim(SettingsManager.maxParallelDownloads, SettingsManager.maxParallelDownloads);
                Console.WriteLine($"[PARALLEL] Initialized with {SettingsManager.maxParallelDownloads} concurrent downloads");
            }
        }

        private void EnsureSemaphoreInitialized()
        {
            if (downloadSemaphore == null)
            {
                Console.WriteLine($"[PARALLEL] WARNING: Semaphore was null, initializing on demand");
                downloadSemaphore = new SemaphoreSlim(SettingsManager.maxParallelDownloads, SettingsManager.maxParallelDownloads);
            }
        }

        public async Task<uint> GetRemoteFileSize(string path)
        {
            path = path.ToLower();
            var found = false;

            foreach (var cdn in cdnList)
            {
                if (found) continue;

                var uri = new Uri("http://" + cdn + "/" + path);
                var cleanName = uri.AbsolutePath;

                try
                {
                    Console.WriteLine($"[HTTP GET - SIZE CHECK] {uri.AbsoluteUri}");
                    using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            found = true;

                            if (response.Content.Headers.ContentLength != null)
                            {
                                var size = (uint)response.Content.Headers.ContentLength;
                                Console.WriteLine($"[HTTP GET - SIZE FOUND] {size} bytes from {cdn}");
                                return size;
                            }
                            else
                            {
                                Console.WriteLine($"[HTTP GET - SIZE FOUND] No content length header from {cdn}");
                                return 0;
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            Logger.WriteLine("File not found on CDN " + cdn + " trying next CDN (if available)..");
                        }
                        else
                        {
                            throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + uri.AbsoluteUri);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("!!! Error retrieving file size " + uri.AbsoluteUri + ": " + e.Message);
                }
            }
            if (!found)
            {
                Console.WriteLine($"[HTTP GET - SIZE NOT FOUND] File {Path.GetFileNameWithoutExtension(path)} not found on any CDN");
                Logger.WriteLine("Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve filesize!", true);
            }

            return 0;
        }

        public async Task<byte[]> Get(string path, bool returnstream = true, bool redownload = false, uint expectedSize = 0, bool verbose = false)
        {
            path = path.ToLower();
            var localPath = Path.Combine(cacheDir, path);

            // Debug: Log all file requests
            Console.WriteLine($"[CDN GET] Requesting: {path}");

            if (File.Exists(localPath) && expectedSize != 0)
            {
                var fileInfo = new FileInfo(localPath);
                if (fileInfo.Length != expectedSize)
                {
                    if (verbose)
                        Console.WriteLine("File size of " + path + " " + fileInfo.Length + " does not match expected size " + expectedSize + " redownloading..");

                    redownload = true;
                }
            }

            var usingCache = !redownload && File.Exists(Path.Combine(cacheDir, path));
            if (usingCache)
            {
                var cachedUrl = "http://" + (cdnList.Count > 0 ? cdnList[0] : "unknown") + "/" + path;
                Console.WriteLine($"[HTTP GET - CACHED] {cachedUrl} (skipping download)");
            }

            if (redownload || !File.Exists(Path.Combine(cacheDir, path)))
            {
                // Ensure semaphore is initialized
                EnsureSemaphoreInitialized();

                // Wait for available download slot
                await downloadSemaphore.WaitAsync();
                Console.WriteLine($"[PARALLEL] Acquired download slot for {path}");

                try
                {
                    var found = false;

                foreach (var cdn in cdnList)
                {
                    if (found) continue;

                    var uri = new Uri("http://" + cdn + "/" + path);
                    var cleanName = uri.AbsolutePath;

                    if (verbose)
                        Console.WriteLine("Downloading " + path + " from " + cdn);

                    try
                    {
                        if (!Directory.Exists(cacheDir + cleanName))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanName));
                        }

                        // Check if partial file exists for resume
                        var fullPath = cacheDir + cleanName;
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

                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            if (response.IsSuccessStatusCode || (canResume && response.StatusCode == System.Net.HttpStatusCode.PartialContent))
                            {
                                // Use append mode if resuming, create new if starting fresh
                                using (var file = canResume ? File.OpenWrite(fullPath) : File.Create(fullPath))
                                {
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
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                Logger.WriteLine("File not found on CDN " + cdn + " trying next CDN (if available)..");
                            }
                            else
                            {
                                throw new FileNotFoundException("Error retrieving file: HTTP status code " +
                                                                response.StatusCode + " on URL " + uri.AbsoluteUri);
                            }
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        if (!e.CancellationToken.IsCancellationRequested)
                        {
                            Logger.WriteLine("!!! Timeout while retrieving file " + uri.AbsoluteUri);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.WriteLine("!!! Error retrieving file " + uri.AbsoluteUri + ": " + e.Message);
                    }
                }

                    if (!found)
                    {
                        Console.WriteLine($"[HTTP GET - FAILED] File {Path.GetFileNameWithoutExtension(path)} not found on any CDN");
                        Logger.WriteLine("Exhausted all CDNs looking for file " + Path.GetFileNameWithoutExtension(path) + ", cannot retrieve it!", true);
                    }
                    else
                    {
                        if (verbose)
                            Console.WriteLine("Downloaded " + path);
                    }
                }
                finally
                {
                    // Release download slot
                    downloadSemaphore.Release();
                    Console.WriteLine($"[PARALLEL] Released download slot for {path}");
                }
            }

            if (returnstream)
            {
                if (path.Contains("tpr/wowdev"))
                {
                    return await Task.FromResult(BLTE.DecryptFile(Path.GetFileNameWithoutExtension(path), await File.ReadAllBytesAsync(Path.Combine(cacheDir, path)), "wowdevalpha"));
                }
                else if (path.Contains("tpr/fenrisdev"))
                {
                    return await Task.FromResult(BLTE.DecryptFile(Path.GetFileNameWithoutExtension(path), await File.ReadAllBytesAsync(Path.Combine(cacheDir, path)), "fenrisdev"));
                }
                else if (path.Contains("tpr/fenrisevent"))
                {
                    return await Task.FromResult(BLTE.DecryptFile(Path.GetFileNameWithoutExtension(path), await File.ReadAllBytesAsync(Path.Combine(cacheDir, path)), "fenrise"));
                }
                else
                {
                    return await File.ReadAllBytesAsync(Path.Combine(cacheDir, path));
                }
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
