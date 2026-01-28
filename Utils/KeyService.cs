using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace BuildBackup
{
    public static class KeyService
    {
        private const string KeysUrl = "https://github.com/wowdev/TACTKeys/raw/refs/heads/master/WoW.txt";
        private const string LocalKeysFile = "tactkeys.txt";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(24);

        private static readonly Dictionary<ulong, byte[]> keys = new Dictionary<ulong, byte[]>();
        private static readonly Salsa20 salsa = new Salsa20();

        static KeyService()
        {
            if (keys.Count == 0)
            {
                LoadKeys();
            }
        }

        public static Salsa20 SalsaInstance => salsa;

        public static byte[] GetKey(ulong keyName)
        {
            keys.TryGetValue(keyName, out byte[] key);
            return key;
        }

        public static void LoadKeys()
        {
            string content = null;

            // Check if local cache exists and is fresh
            if (File.Exists(LocalKeysFile))
            {
                var fileAge = DateTime.Now - File.GetLastWriteTime(LocalKeysFile);
                if (fileAge < CacheExpiry)
                {
                    content = File.ReadAllText(LocalKeysFile);
                }
            }

            // Download if no valid cache
            if (string.IsNullOrEmpty(content))
            {
                content = DownloadKeys();
            }

            // Parse keys if we have content
            if (!string.IsNullOrEmpty(content))
            {
                ParseKeys(content);
            }
        }

        private static string DownloadKeys()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("User-Agent", "BuildBackup");

                    Console.WriteLine($"[HTTP GET] {KeysUrl}");
                    var response = client.GetAsync(KeysUrl).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to download TACT keys: HTTP {(int)response.StatusCode}");
                        return null;
                    }

                    var content = response.Content.ReadAsStringAsync().Result;

                    // Cache locally
                    File.WriteAllText(LocalKeysFile, content);
                    Console.WriteLine($"Downloaded {keys.Count} TACT keys, cached to {LocalKeysFile}");

                    return content;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to download TACT keys: {e.Message}");

                // Fall back to local file if download fails
                if (File.Exists(LocalKeysFile))
                {
                    Console.WriteLine($"Using cached keys from {LocalKeysFile}");
                    return File.ReadAllText(LocalKeysFile);
                }

                return null;
            }
        }

        private static void ParseKeys(string content)
        {
            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var splitLine = line.Split(' ');
                    if (splitLine.Length < 2) continue;

                    try
                    {
                        var lookup = ulong.Parse(splitLine[0], System.Globalization.NumberStyles.HexNumber);
                        byte[] key = splitLine[1].Trim().ToByteArray();

                        if (!keys.ContainsKey(lookup))
                        {
                            keys.Add(lookup, key);
                        }
                    }
                    catch
                    {
                        // Skip malformed lines
                    }
                }
            }
        }
    }
}
