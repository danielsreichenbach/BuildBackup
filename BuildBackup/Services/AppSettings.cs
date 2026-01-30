using System;
using System.Collections.Generic;
using BuildBackup.Interfaces;
using Microsoft.Extensions.Configuration;

#nullable enable

namespace BuildBackup.Services;

/// <summary>
/// Immutable application settings loaded from configuration.
/// </summary>
public class AppSettings : ISettings
{
    public string CacheDir { get; }
    public IReadOnlyList<string> CheckProducts { get; }
    public IReadOnlyList<string> BackupProducts { get; }
    public bool DownloadPatchFiles { get; }
    public int MaxParallelDownloads { get; }

    /// <summary>
    /// Creates settings from an IConfiguration instance.
    /// </summary>
    /// <param name="configuration">Configuration root containing a "config" section.</param>
    public AppSettings(IConfiguration configuration)
    {
        var config = configuration.GetSection("config");

        CacheDir = config["cacheDir"] ?? "cdn/";
        CheckProducts = config.GetSection("checkProducts").Get<string[]>() ?? Array.Empty<string>();
        BackupProducts = config.GetSection("backupProducts").Get<string[]>() ?? Array.Empty<string>();
        DownloadPatchFiles = config.GetValue<bool>("downloadPatchFiles", true);
        MaxParallelDownloads = Math.Max(1, config.GetValue<int>("maxParallelDownloads", 4));
    }

    /// <summary>
    /// Creates settings with explicit values (for testing).
    /// </summary>
    public AppSettings(
        string cacheDir,
        IReadOnlyList<string>? checkProducts = null,
        IReadOnlyList<string>? backupProducts = null,
        bool downloadPatchFiles = true,
        int maxParallelDownloads = 4)
    {
        CacheDir = cacheDir ?? "cdn/";
        CheckProducts = checkProducts ?? Array.Empty<string>();
        BackupProducts = backupProducts ?? Array.Empty<string>();
        DownloadPatchFiles = downloadPatchFiles;
        MaxParallelDownloads = Math.Max(1, maxParallelDownloads);
    }

    /// <summary>
    /// Loads settings from config.json file.
    /// </summary>
    public static AppSettings LoadFromFile(string filePath = "config.json")
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(filePath, optional: false, reloadOnChange: false)
            .Build();

        return new AppSettings(config);
    }
}
