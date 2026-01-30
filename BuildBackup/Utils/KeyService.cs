using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace BuildBackup
{
    public static class KeyService
    {
        private static readonly Services.TactKeyService _service;

        /// <summary>
        /// Gets the underlying IKeyService instance for dependency injection scenarios.
        /// </summary>
        public static Interfaces.IKeyService Instance => _service;

        static KeyService()
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BuildBackup");

            _service = new Services.TactKeyService(httpClient);
            _service.LoadKeys();
        }

        public static Salsa20 SalsaInstance => _service.SalsaInstance;

        public static byte[] GetKey(ulong keyName)
        {
            return _service.GetKey(keyName);
        }

        /// <summary>
        /// Reloads keys. Provided for backward compatibility.
        /// </summary>
        public static void LoadKeys()
        {
            _service.LoadKeys();
        }
    }
}
