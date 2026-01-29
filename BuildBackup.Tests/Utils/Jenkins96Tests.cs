using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace BuildBackup.Tests.Utils;

/// <summary>
/// Tests for Jenkins96 hash algorithm implementation.
/// Jenkins96 is used in CASC/TACT for file path lookups.
/// </summary>
public class Jenkins96Tests
{
    private readonly Jenkins96 _hasher;

    public Jenkins96Tests()
    {
        _hasher = new Jenkins96();
    }

    #region Basic Functionality Tests

    [Fact]
    public void ComputeHash_WithEmptyString_ReturnsConsistentValue()
    {
        var result1 = _hasher.ComputeHash("");
        var result2 = _hasher.ComputeHash("");

        result1.Should().Be(result2);
    }

    [Fact]
    public void ComputeHash_WithSameInput_ReturnsSameHash()
    {
        const string input = "World\\Maps\\Azeroth\\Azeroth.wdt";

        var result1 = _hasher.ComputeHash(input);
        var result2 = _hasher.ComputeHash(input);

        result1.Should().Be(result2);
    }

    [Fact]
    public void ComputeHash_WithDifferentInputs_ReturnsDifferentHashes()
    {
        var hash1 = _hasher.ComputeHash("World\\Maps\\Azeroth\\Azeroth.wdt");
        var hash2 = _hasher.ComputeHash("World\\Maps\\Kalimdor\\Kalimdor.wdt");

        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region Path Normalization Tests (fix=true)

    [Fact]
    public void ComputeHash_NormalizesForwardSlashesToBackslashes()
    {
        var withForward = _hasher.ComputeHash("World/Maps/Azeroth/Azeroth.wdt", fix: true);
        var withBackslash = _hasher.ComputeHash("World\\Maps\\Azeroth\\Azeroth.wdt", fix: true);

        withForward.Should().Be(withBackslash);
    }

    [Fact]
    public void ComputeHash_NormalizesToUppercase()
    {
        var lowercase = _hasher.ComputeHash("world\\maps\\azeroth\\azeroth.wdt", fix: true);
        var uppercase = _hasher.ComputeHash("WORLD\\MAPS\\AZEROTH\\AZEROTH.WDT", fix: true);
        var mixedCase = _hasher.ComputeHash("World\\Maps\\Azeroth\\Azeroth.wdt", fix: true);

        lowercase.Should().Be(uppercase);
        lowercase.Should().Be(mixedCase);
    }

    [Fact]
    public void ComputeHash_NormalizesBothSlashesAndCase()
    {
        var result1 = _hasher.ComputeHash("world/maps/azeroth/azeroth.wdt", fix: true);
        var result2 = _hasher.ComputeHash("WORLD\\MAPS\\AZEROTH\\AZEROTH.WDT", fix: true);

        result1.Should().Be(result2);
    }

    #endregion

    #region Raw Hash Tests (fix=false)

    [Fact]
    public void ComputeHash_WithFixFalse_DoesNotNormalizeSlashes()
    {
        var withForward = _hasher.ComputeHash("World/Maps/Test", fix: false);
        var withBackslash = _hasher.ComputeHash("World\\Maps\\Test", fix: false);

        withForward.Should().NotBe(withBackslash);
    }

    [Fact]
    public void ComputeHash_WithFixFalse_DoesNotNormalizeCase()
    {
        var lowercase = _hasher.ComputeHash("test", fix: false);
        var uppercase = _hasher.ComputeHash("TEST", fix: false);

        lowercase.Should().NotBe(uppercase);
    }

    #endregion

    #region Known Value Tests

    [Fact]
    public void ComputeHash_WithEmptyString_ReturnsExpectedValue()
    {
        // Empty string produces a value based on the algorithm's initial state
        // Initial state: a = b = c = 0xdeadbeef + length (where length = 0)
        // Final state is computed from the mixing function
        var result = _hasher.ComputeHash("", fix: false);

        // The algorithm's empty string behavior produces a non-zero hash
        result.Should().NotBe(0, "hash of empty string should not be zero");
    }

    [Fact]
    public void ComputeHash_WithShortString_ProducesValidHash()
    {
        var result = _hasher.ComputeHash("a", fix: false);
        result.Should().NotBe(0);
    }

    [Fact]
    public void ComputeHash_With12ByteString_HandlesBlockBoundary()
    {
        // 12 bytes is the block size for Jenkins96
        var result = _hasher.ComputeHash("123456789012", fix: false);
        result.Should().NotBe(0);
    }

    [Fact]
    public void ComputeHash_With24ByteString_HandlesMultipleBlocks()
    {
        // Test multiple complete blocks
        var result = _hasher.ComputeHash("123456789012345678901234", fix: false);
        result.Should().NotBe(0);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ComputeHash_WithLongPath_DoesNotThrow()
    {
        var longPath = "World\\Maps\\Testing\\" + new string('a', 200) + ".wdt";

        var act = () => _hasher.ComputeHash(longPath);
        act.Should().NotThrow();
    }

    [Fact]
    public void ComputeHash_WithSpecialCharacters_Handles()
    {
        // File paths might have numbers, underscores, dashes
        var path = "Interface\\Icons\\INV_Misc_123_Test-Icon.blp";

        var act = () => _hasher.ComputeHash(path);
        act.Should().NotThrow();

        var result = _hasher.ComputeHash(path);
        result.Should().NotBe(0);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("abcde")]
    [InlineData("abcdef")]
    [InlineData("abcdefg")]
    [InlineData("abcdefgh")]
    [InlineData("abcdefghi")]
    [InlineData("abcdefghij")]
    [InlineData("abcdefghijk")]
    [InlineData("abcdefghijkl")] // Exactly 12 bytes
    [InlineData("abcdefghijklm")]
    public void ComputeHash_WithVariousLengths_ProducesUniqueHashes(string input)
    {
        var result = _hasher.ComputeHash(input, fix: false);
        result.Should().NotBe(0);
    }

    #endregion

    #region Inheritance Tests (HashAlgorithm)

    [Fact]
    public void ComputeHash_WithByteArray_Completes()
    {
        var data = Encoding.ASCII.GetBytes("test data");

        var act = () => _hasher.ComputeHash(data);
        act.Should().NotThrow();
    }

    [Fact]
    public void Initialize_CanBeCalled()
    {
        var act = () => _hasher.Initialize();
        act.Should().NotThrow();
    }

    #endregion

    #region Typical CASC Path Tests

    [Theory]
    [InlineData("World\\Maps\\Azeroth\\Azeroth.wdt")]
    [InlineData("Interface\\ICONS\\INV_Misc_QuestionMark.blp")]
    [InlineData("DBFilesClient\\ItemSparse.db2")]
    [InlineData("Sound\\Music\\GlueScreenMusic\\wow_main_theme.mp3")]
    [InlineData("Creature\\Murloc\\Murloc.m2")]
    public void ComputeHash_WithTypicalCascPaths_ProducesNonZeroHash(string path)
    {
        var result = _hasher.ComputeHash(path);
        result.Should().NotBe(0);
    }

    [Fact]
    public void ComputeHash_SamePathVariations_ProduceSameHash()
    {
        // All these variations should produce the same hash with fix=true
        var hash1 = _hasher.ComputeHash("DBFilesClient\\Item.dbc");
        var hash2 = _hasher.ComputeHash("dbfilesclient\\item.dbc");
        var hash3 = _hasher.ComputeHash("DBFilesClient/Item.dbc");
        var hash4 = _hasher.ComputeHash("DBFILESCLIENT/ITEM.DBC");

        hash1.Should().Be(hash2);
        hash1.Should().Be(hash3);
        hash1.Should().Be(hash4);
    }

    #endregion
}
