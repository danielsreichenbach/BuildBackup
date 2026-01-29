using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace BuildBackup.Tests.Utils;

/// <summary>
/// Tests for BinaryReaderExtensions and CStringExtensions.
/// These extensions provide endian-aware binary I/O for CASC file parsing.
/// </summary>
public class BinaryReaderExtensionsTests
{
    #region Helper Methods

    private static BinaryReader CreateReader(byte[] data)
    {
        return new BinaryReader(new MemoryStream(data));
    }

    private static BinaryWriter CreateWriter(out MemoryStream stream)
    {
        stream = new MemoryStream();
        return new BinaryWriter(stream);
    }

    #endregion

    #region ReadInt16 Tests

    [Fact]
    public void ReadInt16_LittleEndian_ReadsCorrectly()
    {
        // 0x0102 in little-endian: 02 01
        var data = new byte[] { 0x02, 0x01 };
        using var reader = CreateReader(data);

        var result = reader.ReadInt16(invertEndian: false);

        result.Should().Be(0x0102);
    }

    [Fact]
    public void ReadInt16_BigEndian_ReadsCorrectly()
    {
        // 0x0102 in big-endian: 01 02
        var data = new byte[] { 0x01, 0x02 };
        using var reader = CreateReader(data);

        var result = reader.ReadInt16(invertEndian: true);

        result.Should().Be(0x0102);
    }

    [Fact]
    public void ReadInt16_NegativeValue_ReadsCorrectly()
    {
        // -1 in little-endian: FF FF
        var data = new byte[] { 0xFF, 0xFF };
        using var reader = CreateReader(data);

        var result = reader.ReadInt16(invertEndian: false);

        result.Should().Be(-1);
    }

    #endregion

    #region ReadInt32 Tests

    [Fact]
    public void ReadInt32_LittleEndian_ReadsCorrectly()
    {
        // 0x01020304 in little-endian: 04 03 02 01
        var data = new byte[] { 0x04, 0x03, 0x02, 0x01 };
        using var reader = CreateReader(data);

        var result = reader.ReadInt32(invertEndian: false);

        result.Should().Be(0x01020304);
    }

    [Fact]
    public void ReadInt32_BigEndian_ReadsCorrectly()
    {
        // 0x01020304 in big-endian: 01 02 03 04
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var reader = CreateReader(data);

        var result = reader.ReadInt32(invertEndian: true);

        result.Should().Be(0x01020304);
    }

    #endregion

    #region ReadInt64 Tests

    [Fact]
    public void ReadInt64_LittleEndian_ReadsCorrectly()
    {
        // 0x0102030405060708 in little-endian
        var data = new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 };
        using var reader = CreateReader(data);

        var result = reader.ReadInt64(invertEndian: false);

        result.Should().Be(0x0102030405060708);
    }

    [Fact]
    public void ReadInt64_BigEndian_ReadsCorrectly()
    {
        // 0x0102030405060708 in big-endian
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        using var reader = CreateReader(data);

        var result = reader.ReadInt64(invertEndian: true);

        result.Should().Be(0x0102030405060708);
    }

    #endregion

    #region ReadUInt16 Tests

    [Fact]
    public void ReadUInt16_LittleEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0xFF, 0x7F }; // 32767 in little-endian
        using var reader = CreateReader(data);

        var result = reader.ReadUInt16(invertEndian: false);

        result.Should().Be(0x7FFF);
    }

    [Fact]
    public void ReadUInt16_BigEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0x7F, 0xFF }; // 32767 in big-endian
        using var reader = CreateReader(data);

        var result = reader.ReadUInt16(invertEndian: true);

        result.Should().Be(0x7FFF);
    }

    [Fact]
    public void ReadUInt16_MaxValue_ReadsCorrectly()
    {
        var data = new byte[] { 0xFF, 0xFF };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt16(invertEndian: false);

        result.Should().Be(ushort.MaxValue);
    }

    #endregion

    #region ReadUInt32 Tests

    [Fact]
    public void ReadUInt32_LittleEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt32(invertEndian: false);

        result.Should().Be(0x12345678u);
    }

    [Fact]
    public void ReadUInt32_BigEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt32(invertEndian: true);

        result.Should().Be(0x12345678u);
    }

    #endregion

    #region ReadUInt64 Tests

    [Fact]
    public void ReadUInt64_LittleEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt64(invertEndian: false);

        result.Should().Be(0x0123456789ABCDEFuL);
    }

    [Fact]
    public void ReadUInt64_BigEndian_ReadsCorrectly()
    {
        var data = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt64(invertEndian: true);

        result.Should().Be(0x0123456789ABCDEFuL);
    }

    #endregion

    #region ReadUInt40 Tests (5-byte integer)

    [Fact]
    public void ReadUInt40_LittleEndian_ReadsCorrectly()
    {
        // 5 bytes: 0x0102030405 in little-endian: 05 04 03 02 01
        var data = new byte[] { 0x05, 0x04, 0x03, 0x02, 0x01 };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt40(invertEndian: false);

        result.Should().Be(0x0102030405uL);
    }

    [Fact]
    public void ReadUInt40_BigEndian_ReadsCorrectly()
    {
        // 5 bytes: 0x0102030405 in big-endian: 01 02 03 04 05
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt40(invertEndian: true);

        result.Should().Be(0x0102030405uL);
    }

    [Fact]
    public void ReadUInt40_MaxValue_ReadsCorrectly()
    {
        // Max 40-bit value: FF FF FF FF FF
        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt40(invertEndian: false);

        result.Should().Be(0xFFFFFFFFFFuL);
    }

    [Fact]
    public void ReadUInt40_Zero_ReadsCorrectly()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var reader = CreateReader(data);

        var result = reader.ReadUInt40(invertEndian: false);

        result.Should().Be(0uL);
    }

    #endregion

    #region ReadSingle Tests

    [Fact]
    public void ReadSingle_LittleEndian_ReadsCorrectly()
    {
        // 1.0f in little-endian IEEE 754
        var data = new byte[] { 0x00, 0x00, 0x80, 0x3F };
        using var reader = CreateReader(data);

        var result = reader.ReadSingle(invertEndian: false);

        result.Should().Be(1.0f);
    }

    [Fact]
    public void ReadSingle_BigEndian_ReadsCorrectly()
    {
        // 1.0f in big-endian IEEE 754
        var data = new byte[] { 0x3F, 0x80, 0x00, 0x00 };
        using var reader = CreateReader(data);

        var result = reader.ReadSingle(invertEndian: true);

        result.Should().Be(1.0f);
    }

    #endregion

    #region ReadDouble Tests

    [Fact]
    public void ReadDouble_LittleEndian_ReadsCorrectly()
    {
        // 1.0 in little-endian IEEE 754
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F };
        using var reader = CreateReader(data);

        var result = reader.ReadDouble(invertEndian: false);

        result.Should().Be(1.0);
    }

    [Fact]
    public void ReadDouble_BigEndian_ReadsCorrectly()
    {
        // 1.0 in big-endian IEEE 754
        var data = new byte[] { 0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        using var reader = CreateReader(data);

        var result = reader.ReadDouble(invertEndian: true);

        result.Should().Be(1.0);
    }

    #endregion

    #region CString Tests

    [Fact]
    public void ReadCString_SimpleString_ReadsCorrectly()
    {
        var data = Encoding.UTF8.GetBytes("Hello\0");
        using var reader = CreateReader(data);

        var result = reader.ReadCString();

        result.Should().Be("Hello");
    }

    [Fact]
    public void ReadCString_EmptyString_ReadsCorrectly()
    {
        var data = new byte[] { 0x00 };
        using var reader = CreateReader(data);

        var result = reader.ReadCString();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadCString_WithUnicode_ReadsCorrectly()
    {
        var data = Encoding.UTF8.GetBytes("Héllo Wörld\0");
        using var reader = CreateReader(data);

        var result = reader.ReadCString();

        result.Should().Be("Héllo Wörld");
    }

    [Fact]
    public void ReadCString_MultipleCStrings_ReadsSequentially()
    {
        var str1 = "First\0";
        var str2 = "Second\0";
        var data = Encoding.UTF8.GetBytes(str1 + str2);
        using var reader = CreateReader(data);

        var result1 = reader.ReadCString();
        var result2 = reader.ReadCString();

        result1.Should().Be("First");
        result2.Should().Be("Second");
    }

    [Fact]
    public void ReadCString_WithCustomEncoding_ReadsCorrectly()
    {
        var data = Encoding.ASCII.GetBytes("Test\0");
        using var reader = CreateReader(data);

        var result = reader.ReadCString(Encoding.ASCII);

        result.Should().Be("Test");
    }

    [Fact]
    public void WriteCString_SimpleString_WritesCorrectly()
    {
        using var writer = CreateWriter(out var stream);

        writer.WriteCString("Hello");
        writer.Flush();

        var result = stream.ToArray();
        result.Should().BeEquivalentTo(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00 });
    }

    [Fact]
    public void WriteCString_EmptyString_WritesNullTerminator()
    {
        using var writer = CreateWriter(out var stream);

        writer.WriteCString("");
        writer.Flush();

        var result = stream.ToArray();
        result.Should().BeEquivalentTo(new byte[] { 0x00 });
    }

    [Fact]
    public void WriteCString_ThenReadCString_RoundTrips()
    {
        const string original = "Test String 123";

        using var writer = CreateWriter(out var stream);
        writer.WriteCString(original);
        writer.Flush();

        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var result = reader.ReadCString();

        result.Should().Be(original);
    }

    #endregion

    #region ToByteArray Tests

    [Fact]
    public void ToByteArray_SimpleHex_ConvertsCorrectly()
    {
        var result = "DEADBEEF".ToByteArray();

        result.Should().BeEquivalentTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
    }

    [Fact]
    public void ToByteArray_LowercaseHex_ConvertsCorrectly()
    {
        var result = "deadbeef".ToByteArray();

        result.Should().BeEquivalentTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
    }

    [Fact]
    public void ToByteArray_WithSpaces_IgnoresSpaces()
    {
        var result = "DE AD BE EF".ToByteArray();

        result.Should().BeEquivalentTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
    }

    [Fact]
    public void ToByteArray_EmptyString_ReturnsEmptyArray()
    {
        var result = "".ToByteArray();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ToByteArray_Md5Hash_ConvertsCorrectly()
    {
        // Typical MD5 hash format
        var result = "6554ef4e3835fd30453cfda4ac014ff0".ToByteArray();

        result.Should().HaveCount(16);
        result[0].Should().Be(0x65);
        result[15].Should().Be(0xF0);
    }

    #endregion

    #region Sequential Reading Tests

    [Fact]
    public void ReadMultipleValues_AdvancesPosition()
    {
        // Create data with multiple values
        var data = new byte[]
        {
            0x01, 0x00,             // UInt16 = 1
            0x02, 0x00, 0x00, 0x00, // UInt32 = 2
            0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 // UInt64 = 3
        };
        using var reader = CreateReader(data);

        var val1 = reader.ReadUInt16(invertEndian: false);
        var val2 = reader.ReadUInt32(invertEndian: false);
        var val3 = reader.ReadUInt64(invertEndian: false);

        val1.Should().Be(1);
        val2.Should().Be(2u);
        val3.Should().Be(3uL);
    }

    #endregion
}
