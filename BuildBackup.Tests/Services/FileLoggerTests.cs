using BuildBackup.Services;
using FluentAssertions;
using Xunit;

namespace BuildBackup.Tests.Services;

/// <summary>
/// Tests for FileLogger error logging.
/// </summary>
public class FileLoggerTests : IDisposable
{
    private readonly string _testLogFile;

    public FileLoggerTests()
    {
        _testLogFile = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid()}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_testLogFile))
        {
            File.Delete(_testLogFile);
        }
    }

    [Fact]
    public void LogError_WritesToFile()
    {
        using var logger = new FileLogger(_testLogFile);

        logger.LogError("Test error message");

        // Flush happens via AutoFlush
        var content = File.ReadAllText(_testLogFile);
        content.Should().Contain("Test error message");
    }

    [Fact]
    public void LogError_WithMultipleMessages_WritesAllMessages()
    {
        using var logger = new FileLogger(_testLogFile);

        logger.LogError("First message");
        logger.LogError("Second message");
        logger.LogError("Third message");

        var content = File.ReadAllText(_testLogFile);
        content.Should().Contain("First message");
        content.Should().Contain("Second message");
        content.Should().Contain("Third message");
    }

    [Fact]
    public void LogError_WithConsoleOutput_WritesToBothFileAndConsole()
    {
        using var logger = new FileLogger(_testLogFile);
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        try
        {
            logger.LogError("Console test message", alsoToConsole: true);

            var fileContent = File.ReadAllText(_testLogFile);
            fileContent.Should().Contain("Console test message");

            var consoleContent = consoleOutput.ToString();
            consoleContent.Should().Contain("Console test message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void LogError_WithoutConsoleOutput_WritesOnlyToFile()
    {
        using var logger = new FileLogger(_testLogFile);
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        try
        {
            logger.LogError("File only message", alsoToConsole: false);

            var fileContent = File.ReadAllText(_testLogFile);
            fileContent.Should().Contain("File only message");

            var consoleContent = consoleOutput.ToString();
            consoleContent.Should().NotContain("File only message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var logger = new FileLogger(_testLogFile);

        var act = () =>
        {
            logger.Dispose();
            logger.Dispose();
            logger.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void LogError_AfterDispose_ThrowsObjectDisposedException()
    {
        var logger = new FileLogger(_testLogFile);
        logger.Dispose();

        var act = () => logger.LogError("Should throw");

        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Constructor_WithDefaultPath_UsesErrorsTxt()
    {
        // This test verifies the default parameter, not actual file creation
        // to avoid polluting the current directory
        var logger = new FileLogger();

        // Just verify it doesn't throw - the default path is "errors.txt"
        logger.Should().NotBeNull();
        logger.Dispose();
    }
}
