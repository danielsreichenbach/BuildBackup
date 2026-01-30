using System;

namespace BuildBackup.Interfaces;

/// <summary>
/// Application logging interface for error logging.
/// </summary>
public interface IAppLogger : IDisposable
{
    /// <summary>
    /// Logs an error message to the log file.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogError(string message);

    /// <summary>
    /// Logs an error message, optionally also to console.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="alsoToConsole">If true, also writes to console.</param>
    void LogError(string message, bool alsoToConsole);
}
