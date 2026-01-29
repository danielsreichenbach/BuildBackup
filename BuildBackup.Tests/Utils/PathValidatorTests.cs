using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace BuildBackup.Tests.Utils;

/// <summary>
/// Tests for PathValidator utility class.
/// </summary>
public class PathValidatorTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testFile;

    public PathValidatorTests()
    {
        // Create temp directory and file for tests
        _testDir = Path.Combine(Path.GetTempPath(), $"PathValidatorTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _testFile = Path.Combine(_testDir, "testfile.txt");
        File.WriteAllText(_testFile, "test content");
    }

    public void Dispose()
    {
        // Cleanup
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region ValidateNoTraversal Tests

    [Theory]
    [InlineData("file.txt")]
    [InlineData("subdir/file.txt")]
    [InlineData("subdir\\file.txt")]
    [InlineData("my-file_123.dat")]
    public void ValidateNoTraversal_WithValidPath_DoesNotThrow(string path)
    {
        var act = () => PathValidator.ValidateNoTraversal(path, "testParam");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ValidateNoTraversal_WithNullOrWhitespace_ThrowsArgumentException(string? path)
    {
        var act = () => PathValidator.ValidateNoTraversal(path!, "testParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("testParam")
            .WithMessage("*cannot be null or empty*");
    }

    [Theory]
    [InlineData("../file.txt")]
    [InlineData("..\\file.txt")]
    [InlineData("subdir/../../../etc/passwd")]
    [InlineData("foo/bar/../../../secret")]
    public void ValidateNoTraversal_WithTraversalSequence_ThrowsArgumentException(string path)
    {
        var act = () => PathValidator.ValidateNoTraversal(path, "testParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("testParam")
            .WithMessage("*invalid traversal sequence*");
    }

    [Fact]
    public void ValidateNoTraversal_WithAbsoluteUnixPath_DoesNotThrow()
    {
        // Absolute paths without traversal are allowed (validated but not rejected)
        var act = () => PathValidator.ValidateNoTraversal("/home/user/file.txt", "testParam");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateNoTraversal_WithTilde_DoesNotThrow()
    {
        // Tilde without traversal is allowed (validated but not rejected)
        var act = () => PathValidator.ValidateNoTraversal("~user/file.txt", "testParam");
        act.Should().NotThrow();
    }

    #endregion

    #region ValidateOutputPath Tests

    [Fact]
    public void ValidateOutputPath_WithValidPathWithinBase_ReturnsAbsolutePath()
    {
        var result = PathValidator.ValidateOutputPath("output.txt", _testDir, "outParam");

        result.Should().StartWith(_testDir);
        result.Should().EndWith("output.txt");
    }

    [Fact]
    public void ValidateOutputPath_WithSubdirectory_ReturnsAbsolutePath()
    {
        var result = PathValidator.ValidateOutputPath("sub/output.txt", _testDir, "outParam");

        result.Should().StartWith(_testDir);
        result.Should().Contain("sub");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOutputPath_WithNullOrEmptyOutputPath_ThrowsArgumentException(string? outputPath)
    {
        var act = () => PathValidator.ValidateOutputPath(outputPath!, _testDir, "outParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("outParam")
            .WithMessage("*Output path cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOutputPath_WithNullOrEmptyBaseDirectory_ThrowsArgumentException(string? baseDir)
    {
        var act = () => PathValidator.ValidateOutputPath("file.txt", baseDir!, "outParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseDirectory")
            .WithMessage("*Base directory cannot be null or empty*");
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("../../escape.txt")]
    [InlineData("sub/../../escape.txt")]
    public void ValidateOutputPath_WithTraversalEscapingBase_ThrowsArgumentException(string outputPath)
    {
        var act = () => PathValidator.ValidateOutputPath(outputPath, _testDir, "outParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("outParam")
            .WithMessage("*would escape base directory*");
    }

    #endregion

    #region ValidateHash Tests

    [Theory]
    [InlineData("abcdef1234567890abcdef1234567890", 32)]
    [InlineData("ABCDEF1234567890ABCDEF1234567890", 32)]
    [InlineData("AbCdEf1234567890AbCdEf1234567890", 32)]
    [InlineData("0123456789abcdef", 16)]
    [InlineData("ff", 2)]
    public void ValidateHash_WithValidHexAndLength_DoesNotThrow(string hash, int expectedLength)
    {
        var act = () => PathValidator.ValidateHash(hash, expectedLength, "hashParam");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("0123456789ABCDEF0123456789ABCDEF")]
    [InlineData("a")]
    public void ValidateHash_WithAnyLengthAllowed_DoesNotThrow(string hash)
    {
        var act = () => PathValidator.ValidateHash(hash, 0, "hashParam");
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateHash_WithNullOrWhitespace_ThrowsArgumentException(string? hash)
    {
        var act = () => PathValidator.ValidateHash(hash!, 32, "hashParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hashParam")
            .WithMessage("*cannot be null or empty*");
    }

    [Theory]
    [InlineData("ghijklmnop")]           // Non-hex letters
    [InlineData("12345xyz67890")]         // Mixed valid/invalid
    [InlineData("abcdef!@#$")]           // Special characters
    [InlineData("abcdef 123456")]         // Space in middle
    [InlineData("abcdef\t123456")]        // Tab character
    public void ValidateHash_WithInvalidCharacters_ThrowsArgumentException(string hash)
    {
        var act = () => PathValidator.ValidateHash(hash, 0, "hashParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hashParam")
            .WithMessage("*invalid characters*hexadecimal*");
    }

    [Theory]
    [InlineData("abcdef", 32)]            // Too short
    [InlineData("abcdef1234567890abcdef1234567890abcd", 32)]  // Too long
    [InlineData("ab", 32)]
    public void ValidateHash_WithWrongLength_ThrowsArgumentException(string hash, int expectedLength)
    {
        var act = () => PathValidator.ValidateHash(hash, expectedLength, "hashParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("hashParam")
            .WithMessage($"*invalid length {hash.Length}*expected {expectedLength}*");
    }

    [Fact]
    public void ValidateHash_WithRealMd5Hash_DoesNotThrow()
    {
        // Real MD5 hash format: 32 hex characters
        const string md5Hash = "6554ef4e3835fd30453cfda4ac014ff0";
        var act = () => PathValidator.ValidateHash(md5Hash, 32, "buildconfig");
        act.Should().NotThrow();
    }

    #endregion

    #region ValidateInputFile Tests

    [Fact]
    public void ValidateInputFile_WithExistingFile_ReturnsAbsolutePath()
    {
        var result = PathValidator.ValidateInputFile(_testFile, "fileParam");

        result.Should().Be(_testFile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateInputFile_WithNullOrWhitespace_ThrowsArgumentException(string? filePath)
    {
        var act = () => PathValidator.ValidateInputFile(filePath!, "fileParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fileParam")
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void ValidateInputFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistent = Path.Combine(_testDir, "does_not_exist.txt");

        var act = () => PathValidator.ValidateInputFile(nonExistent, "fileParam");
        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Input file not found*");
    }

    #endregion

    #region ValidateInputDirectory Tests

    [Fact]
    public void ValidateInputDirectory_WithExistingDirectory_ReturnsAbsolutePath()
    {
        var result = PathValidator.ValidateInputDirectory(_testDir, "dirParam");

        result.Should().Be(_testDir);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateInputDirectory_WithNullOrWhitespace_ThrowsArgumentException(string? dirPath)
    {
        var act = () => PathValidator.ValidateInputDirectory(dirPath!, "dirParam");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("dirParam")
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void ValidateInputDirectory_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var nonExistent = Path.Combine(_testDir, "does_not_exist_dir");

        var act = () => PathValidator.ValidateInputDirectory(nonExistent, "dirParam");
        act.Should().Throw<DirectoryNotFoundException>()
            .WithMessage("*Input directory not found*");
    }

    #endregion
}
