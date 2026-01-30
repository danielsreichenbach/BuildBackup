using BuildBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BuildBackup.Tests.Services;

/// <summary>
/// Tests for AppSettings configuration loading.
/// </summary>
public class AppSettingsTests
{
    [Fact]
    public void Constructor_WithExplicitValues_SetsProperties()
    {
        var settings = new AppSettings(
            cacheDir: "test/",
            checkProducts: new[] { "wow" },
            backupProducts: new[] { "wow_classic" },
            downloadPatchFiles: false,
            maxParallelDownloads: 8);

        settings.CacheDir.Should().Be("test/");
        settings.CheckProducts.Should().ContainSingle().Which.Should().Be("wow");
        settings.BackupProducts.Should().ContainSingle().Which.Should().Be("wow_classic");
        settings.DownloadPatchFiles.Should().BeFalse();
        settings.MaxParallelDownloads.Should().Be(8);
    }

    [Fact]
    public void Constructor_WithNullCacheDir_UsesDefault()
    {
        var settings = new AppSettings(cacheDir: null!);

        settings.CacheDir.Should().Be("cdn/");
    }

    [Fact]
    public void Constructor_WithNullCheckProducts_UsesEmptyArray()
    {
        var settings = new AppSettings(cacheDir: "test/", checkProducts: null);

        settings.CheckProducts.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullBackupProducts_UsesEmptyArray()
    {
        var settings = new AppSettings(cacheDir: "test/", backupProducts: null);

        settings.BackupProducts.Should().BeEmpty();
    }

    [Fact]
    public void MaxParallelDownloads_WithZero_ReturnsOne()
    {
        var settings = new AppSettings(cacheDir: "test/", maxParallelDownloads: 0);

        settings.MaxParallelDownloads.Should().Be(1);
    }

    [Fact]
    public void MaxParallelDownloads_WithNegative_ReturnsOne()
    {
        var settings = new AppSettings(cacheDir: "test/", maxParallelDownloads: -5);

        settings.MaxParallelDownloads.Should().Be(1);
    }

    [Fact]
    public void Constructor_FromConfiguration_LoadsValues()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "config:cacheDir", "configured/" },
            { "config:checkProducts:0", "product1" },
            { "config:checkProducts:1", "product2" },
            { "config:backupProducts:0", "backup1" },
            { "config:downloadPatchFiles", "false" },
            { "config:maxParallelDownloads", "16" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var settings = new AppSettings(configuration);

        settings.CacheDir.Should().Be("configured/");
        settings.CheckProducts.Should().HaveCount(2);
        settings.CheckProducts.Should().Contain("product1");
        settings.CheckProducts.Should().Contain("product2");
        settings.BackupProducts.Should().ContainSingle().Which.Should().Be("backup1");
        settings.DownloadPatchFiles.Should().BeFalse();
        settings.MaxParallelDownloads.Should().Be(16);
    }

    [Fact]
    public void Constructor_FromEmptyConfiguration_UsesDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var settings = new AppSettings(configuration);

        settings.CacheDir.Should().Be("cdn/");
        settings.CheckProducts.Should().BeEmpty();
        settings.BackupProducts.Should().BeEmpty();
        settings.DownloadPatchFiles.Should().BeTrue(); // Default
        settings.MaxParallelDownloads.Should().Be(4); // Default
    }

}
