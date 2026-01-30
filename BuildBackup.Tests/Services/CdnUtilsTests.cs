using BuildBackup.Interfaces;
using FluentAssertions;
using Xunit;

namespace BuildBackup.Tests.Services;

/// <summary>
/// Tests for CdnUtils utility methods.
/// </summary>
public class CdnUtilsTests
{
    [Theory]
    [InlineData(0UL, "0 B")]
    [InlineData(1UL, "1 B")]
    [InlineData(1023UL, "1023 B")]
    [InlineData(1024UL, "1 KB")]
    [InlineData(1536UL, "1.5 KB")]
    [InlineData(1048576UL, "1 MB")]
    [InlineData(1073741824UL, "1 GB")]
    [InlineData(1099511627776UL, "1 TB")]
    public void FormatFileSize_FormatsCorrectly(ulong bytes, string expected)
    {
        var result = CdnUtils.FormatFileSize(bytes);

        result.Should().Be(expected);
    }

    [Fact]
    public void FormatFileSize_WithLargeValue_FormatsTB()
    {
        // 2.5 TB
        var bytes = 2748779069440UL;

        var result = CdnUtils.FormatFileSize(bytes);

        result.Should().Be("2.5 TB");
    }

    [Fact]
    public void FormatFileSize_WithPrecision_TruncatesToTwoDecimals()
    {
        // 1.333... KB
        var bytes = 1365UL;

        var result = CdnUtils.FormatFileSize(bytes);

        // Should be around 1.33 KB
        result.Should().MatchRegex(@"1\.\d{1,2} KB");
    }
}
