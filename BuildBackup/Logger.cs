using System;
using System.IO;

namespace BuildBackup
{
    public static class Logger
    {
        private static readonly Interfaces.IAppLogger _logger;

        /// <summary>
        /// Gets the underlying IAppLogger instance for dependency injection scenarios.
        /// </summary>
        public static Interfaces.IAppLogger Instance => _logger;

        static Logger()
        {
            _logger = new Services.FileLogger();
        }

        /// <summary>
        /// Writes a line to the error log, optionally also to console.
        /// </summary>
        /// <param name="line">The line to write.</param>
        /// <param name="output">If true, also writes to console.</param>
        public static void WriteLine(string line, bool output = false)
        {
            _logger.LogError(line, output);
        }

        /// <summary>
        /// Disposes the underlying logger. Call at application shutdown.
        /// </summary>
        public static void Shutdown()
        {
            _logger.Dispose();
        }
    }
}
