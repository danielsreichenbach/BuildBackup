using BuildBackup.Services;
using FluentAssertions;
using System.Net;
using System.Net.Http;
using Xunit;

namespace BuildBackup.Tests.Services;

/// <summary>
/// Tests for TactKeyService encryption key management.
/// </summary>
public class TactKeyServiceTests : IDisposable
{
    private readonly string _testCacheFile;

    public TactKeyServiceTests()
    {
        _testCacheFile = Path.Combine(Path.GetTempPath(), $"test_keys_{Guid.NewGuid()}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_testCacheFile))
        {
            File.Delete(_testCacheFile);
        }
    }

    [Fact]
    public async Task LoadKeysAsync_WithCachedFile_DoesNotDownload()
    {
        // Create a fresh cache file
        var keyContent = "FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD\n";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(
            httpClient,
            cacheFilePath: _testCacheFile,
            cacheExpiry: TimeSpan.FromHours(24));

        await service.LoadKeysAsync();

        // Key should be loaded from cache
        service.IsKeyAvailable(0xFA505078126ACB3E).Should().BeTrue();
    }

    [Fact]
    public async Task GetKey_WithExistingKey_ReturnsKeyBytes()
    {
        var keyContent = "FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD\n";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        await service.LoadKeysAsync();
        var key = service.GetKey(0xFA505078126ACB3E);

        key.Should().NotBeNull();
        key.Should().HaveCount(16); // 32 hex chars = 16 bytes
    }

    [Fact]
    public async Task GetKey_WithNonExistingKey_ReturnsNull()
    {
        var keyContent = "FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD\n";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        await service.LoadKeysAsync();
        var key = service.GetKey(0xDEADBEEFDEADBEEF);

        key.Should().BeNull();
    }

    [Fact]
    public async Task IsKeyAvailable_WithExistingKey_ReturnsTrue()
    {
        var keyContent = "FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD\n";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        await service.LoadKeysAsync();

        service.IsKeyAvailable(0xFA505078126ACB3E).Should().BeTrue();
    }

    [Fact]
    public async Task IsKeyAvailable_WithNonExistingKey_ReturnsFalse()
    {
        var keyContent = "FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD\n";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        await service.LoadKeysAsync();

        service.IsKeyAvailable(0xDEADBEEFDEADBEEF).Should().BeFalse();
    }

    [Fact]
    public void SalsaInstance_ReturnsNonNullSalsa20()
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        service.SalsaInstance.Should().NotBeNull();
        service.SalsaInstance.Should().BeOfType<Salsa20>();
    }

    [Fact]
    public async Task LoadKeysAsync_MultipleCallsAreSafe()
    {
        var keyContent = "FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD\n";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        await service.LoadKeysAsync();
        await service.LoadKeysAsync();
        await service.LoadKeysAsync();

        // Should not throw and key should still be available
        service.IsKeyAvailable(0xFA505078126ACB3E).Should().BeTrue();
    }

    [Fact]
    public async Task LoadKeysAsync_WithMalformedLines_SkipsThem()
    {
        var keyContent = @"FA505078126ACB3E 4E4BDECAB8485B4F9F4D67AB9E4BB6DD
invalid line without proper format
another bad line
DB505078126ACB3E 5E5BDECAB8485B4F9F4D67AB9E4BB6DD
";
        await File.WriteAllTextAsync(_testCacheFile, keyContent);

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        await service.LoadKeysAsync();

        // Valid keys should be loaded
        service.IsKeyAvailable(0xFA505078126ACB3E).Should().BeTrue();
        service.IsKeyAvailable(0xDB505078126ACB3E).Should().BeTrue();
    }

    [Fact]
    public async Task LoadKeysAsync_WithEmptyFile_DoesNotThrow()
    {
        await File.WriteAllTextAsync(_testCacheFile, "");

        var httpClient = new HttpClient(new MockHttpMessageHandler(HttpStatusCode.OK, ""));
        var service = new TactKeyService(httpClient, cacheFilePath: _testCacheFile);

        var act = async () => await service.LoadKeysAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        var act = () => new TactKeyService(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Mock HTTP message handler for testing without actual network calls.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}
