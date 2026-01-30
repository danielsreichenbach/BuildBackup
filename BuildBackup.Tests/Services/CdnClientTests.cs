using BuildBackup.Interfaces;
using BuildBackup.Services;
using FluentAssertions;
using System.Net;
using Xunit;

namespace BuildBackup.Tests.Services;

/// <summary>
/// Tests for CdnClient CDN operations.
/// Note: These tests use mocked HTTP responses to avoid actual network calls.
/// </summary>
public class CdnClientTests : IDisposable
{
    private readonly string _testCacheDir;
    private readonly MockAppLogger _mockLogger;

    public CdnClientTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"cdn_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testCacheDir);
        _mockLogger = new MockAppLogger();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testCacheDir))
        {
            Directory.Delete(_testCacheDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var httpClient = new HttpClient();
        var cdnServers = new[] { "cdn1.example.com", "cdn2.example.com" };

        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir, cdnServers, 8);

        client.CacheDir.Should().Be(_testCacheDir);
        client.CdnServers.Should().HaveCount(2);
        client.CdnServers.Should().Contain("cdn1.example.com");
        client.CdnServers.Should().Contain("cdn2.example.com");
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        var act = () => new CdnClient(null!, _mockLogger, _testCacheDir);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var httpClient = new HttpClient();

        var act = () => new CdnClient(httpClient, null!, _testCacheDir);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullCacheDir_ThrowsArgumentNullException()
    {
        var httpClient = new HttpClient();

        var act = () => new CdnClient(httpClient, _mockLogger, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cacheDir");
    }

    [Fact]
    public void AddCdnServer_AddsToList()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);

        client.AddCdnServer("new.cdn.example.com");

        client.CdnServers.Should().Contain("new.cdn.example.com");
    }

    [Fact]
    public void AddCdnServer_WithDuplicate_DoesNotAddAgain()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir, new[] { "cdn.example.com" });

        client.AddCdnServer("cdn.example.com");

        client.CdnServers.Should().HaveCount(1);
    }

    [Fact]
    public void AddCdnServer_WithEmpty_DoesNotAdd()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);

        client.AddCdnServer("");
        client.AddCdnServer(null!);

        client.CdnServers.Should().BeEmpty();
    }

    [Fact]
    public void DecryptionKeyName_CanBeSetAndRead()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);

        client.DecryptionKeyName = "wowdevalpha";

        client.DecryptionKeyName.Should().Be("wowdevalpha");
    }

    [Fact]
    public void InitializeParallelDownloads_CanBeCalledMultipleTimes()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);

        var act = () =>
        {
            client.InitializeParallelDownloads();
            client.InitializeParallelDownloads();
            client.InitializeParallelDownloads();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);
        client.InitializeParallelDownloads();

        var act = () =>
        {
            client.Dispose();
            client.Dispose();
            client.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void ImplementsICdnClient()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);

        client.Should().BeAssignableTo<ICdnClient>();
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir);

        client.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public async Task GetAsync_WithCachedFile_ReturnsCachedContent()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir, new[] { "cdn.example.com" });

        // Pre-create a cached file
        var testPath = "test/file.dat";
        var cachedFilePath = Path.Combine(_testCacheDir, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(cachedFilePath)!);
        var expectedContent = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(cachedFilePath, expectedContent);

        var result = await client.GetAsync(testPath);

        result.Should().BeEquivalentTo(expectedContent);
    }

    [Fact]
    public async Task GetAsync_WithReturnContentFalse_ReturnsEmptyArray()
    {
        var httpClient = new HttpClient();
        var client = new CdnClient(httpClient, _mockLogger, _testCacheDir, new[] { "cdn.example.com" });

        // Pre-create a cached file
        var testPath = "test/file.dat";
        var cachedFilePath = Path.Combine(_testCacheDir, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(cachedFilePath)!);
        await File.WriteAllBytesAsync(cachedFilePath, new byte[] { 1, 2, 3 });

        var options = new CdnGetOptions { ReturnContent = false };
        var result = await client.GetAsync(testPath, options);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Mock logger for testing without actual file I/O.
    /// </summary>
    private class MockAppLogger : IAppLogger
    {
        public List<string> LoggedMessages { get; } = new();
        public bool IsDisposed { get; private set; }

        public void LogError(string message)
        {
            LoggedMessages.Add(message);
        }

        public void LogError(string message, bool alsoToConsole)
        {
            LoggedMessages.Add(message);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
