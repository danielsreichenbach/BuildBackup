using System;
using System.IO;
using BuildBackup.Interfaces;

namespace BuildBackup.Services;

/// <summary>
/// File-based error logger with proper disposal.
/// </summary>
public sealed class FileLogger : IAppLogger
{
    private readonly StreamWriter _writer;
    private bool _disposed;

    /// <summary>
    /// Creates a new file logger.
    /// </summary>
    /// <param name="filePath">Path to the log file.</param>
    public FileLogger(string filePath = "errors.txt")
    {
        _writer = new StreamWriter(File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
    }

    /// <summary>
    /// Logs an error message to the log file.
    /// </summary>
    public void LogError(string message)
    {
        LogError(message, false);
    }

    /// <summary>
    /// Logs an error message, optionally also to console.
    /// </summary>
    public void LogError(string message, bool alsoToConsole)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileLogger));

        _writer.WriteLine(message);

        if (alsoToConsole)
            Console.WriteLine(message);
    }

    /// <summary>
    /// Disposes the underlying stream writer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _writer.Dispose();
        _disposed = true;
    }
}
