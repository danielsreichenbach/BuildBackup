using System;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;

namespace BuildBackup.Tests.Utils;

/// <summary>
/// Tests for Salsa20 stream cipher implementation.
/// Salsa20 is used in CASC/TACT for chunk-level encryption.
/// </summary>
public class Salsa20Tests
{
    #region Key Size Validation Tests

    [Fact]
    public void CreateEncryptor_With128BitKey_Succeeds()
    {
        using var salsa = new Salsa20();
        var key = new byte[16]; // 128 bits
        var iv = new byte[8];

        var act = () => salsa.CreateEncryptor(key, iv);

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateEncryptor_With256BitKey_Succeeds()
    {
        using var salsa = new Salsa20();
        var key = new byte[32]; // 256 bits
        var iv = new byte[8];

        var act = () => salsa.CreateEncryptor(key, iv);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(8)]   // 64 bits
    [InlineData(24)]  // 192 bits
    [InlineData(48)]  // 384 bits
    public void CreateEncryptor_WithInvalidKeySize_ThrowsCryptographicException(int keyBytes)
    {
        using var salsa = new Salsa20();
        var key = new byte[keyBytes];
        var iv = new byte[8];

        var act = () => salsa.CreateEncryptor(key, iv);

        act.Should().Throw<CryptographicException>()
            .WithMessage("*Invalid key size*");
    }

    [Fact]
    public void CreateEncryptor_WithNullKey_ThrowsArgumentNullException()
    {
        using var salsa = new Salsa20();
        var iv = new byte[8];

        var act = () => salsa.CreateEncryptor(null!, iv);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region IV Validation Tests

    [Fact]
    public void CreateEncryptor_With8ByteIV_Succeeds()
    {
        using var salsa = new Salsa20();
        var key = new byte[32];
        var iv = new byte[8]; // Required size

        var act = () => salsa.CreateEncryptor(key, iv);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(16)]
    public void CreateEncryptor_WithInvalidIVSize_ThrowsCryptographicException(int ivBytes)
    {
        using var salsa = new Salsa20();
        var key = new byte[32];
        var iv = new byte[ivBytes];

        var act = () => salsa.CreateEncryptor(key, iv);

        act.Should().Throw<CryptographicException>()
            .WithMessage("*Invalid IV size*");
    }

    [Fact]
    public void CreateEncryptor_WithNullIV_ThrowsArgumentNullException()
    {
        using var salsa = new Salsa20();
        var key = new byte[32];

        var act = () => salsa.CreateEncryptor(key, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IV_SetValidValue_Succeeds()
    {
        using var salsa = new Salsa20();
        var iv = new byte[8];

        var act = () => salsa.IV = iv;

        act.Should().NotThrow();
    }

    [Fact]
    public void IV_SetInvalidSize_ThrowsCryptographicException()
    {
        using var salsa = new Salsa20();
        var iv = new byte[16]; // Wrong size

        var act = () => salsa.IV = iv;

        act.Should().Throw<CryptographicException>();
    }

    #endregion

    #region Rounds Validation Tests

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(20)]
    public void Rounds_SetValidValue_Succeeds(int rounds)
    {
        using var salsa = new Salsa20();

        var act = () => salsa.Rounds = rounds;

        act.Should().NotThrow();
        salsa.Rounds.Should().Be(rounds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(24)]
    public void Rounds_SetInvalidValue_ThrowsArgumentOutOfRangeException(int rounds)
    {
        using var salsa = new Salsa20();

        var act = () => salsa.Rounds = rounds;

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rounds_DefaultValue_Is20()
    {
        using var salsa = new Salsa20();

        salsa.Rounds.Should().Be(20);
    }

    #endregion

    #region Encryption/Decryption Symmetry Tests

    [Fact]
    public void EncryptThenDecrypt_With256BitKey_ReturnsOriginal()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var plaintext = Encoding.UTF8.GetBytes("Hello, Salsa20 World!");

        // Encrypt
        byte[] ciphertext;
        using (var encryptor = salsa.CreateEncryptor(key, iv))
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        // Decrypt (Salsa20 is symmetric - encryption = decryption)
        byte[] decrypted;
        using (var decryptor = salsa.CreateDecryptor(key, iv))
        {
            decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public void EncryptThenDecrypt_With128BitKey_ReturnsOriginal()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(16);
        var iv = GenerateRandomBytes(8);
        var plaintext = Encoding.UTF8.GetBytes("Test with 128-bit key");

        byte[] ciphertext;
        using (var encryptor = salsa.CreateEncryptor(key, iv))
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] decrypted;
        using (var decryptor = salsa.CreateDecryptor(key, iv))
        {
            decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(20)]
    public void EncryptThenDecrypt_WithDifferentRounds_ReturnsOriginal(int rounds)
    {
        using var salsa = new Salsa20 { Rounds = rounds };
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var plaintext = Encoding.UTF8.GetBytes("Testing different round counts");

        byte[] ciphertext;
        using (var encryptor = salsa.CreateEncryptor(key, iv))
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        using var salsa2 = new Salsa20 { Rounds = rounds };
        byte[] decrypted;
        using (var decryptor = salsa2.CreateDecryptor(key, iv))
        {
            decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    #endregion

    #region Key/IV Sensitivity Tests

    [Fact]
    public void Encrypt_WithDifferentKeys_ProducesDifferentCiphertext()
    {
        using var salsa = new Salsa20();
        var key1 = GenerateRandomBytes(32);
        var key2 = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var plaintext = Encoding.UTF8.GetBytes("Same message, different keys");

        byte[] ciphertext1;
        using (var encryptor = salsa.CreateEncryptor(key1, iv))
        {
            ciphertext1 = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] ciphertext2;
        using (var encryptor = salsa.CreateEncryptor(key2, iv))
        {
            ciphertext2 = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        ciphertext1.Should().NotBeEquivalentTo(ciphertext2);
    }

    [Fact]
    public void Encrypt_WithDifferentIVs_ProducesDifferentCiphertext()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv1 = GenerateRandomBytes(8);
        var iv2 = GenerateRandomBytes(8);
        var plaintext = Encoding.UTF8.GetBytes("Same message, different IVs");

        byte[] ciphertext1;
        using (var encryptor = salsa.CreateEncryptor(key, iv1))
        {
            ciphertext1 = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        byte[] ciphertext2;
        using (var encryptor = salsa.CreateEncryptor(key, iv2))
        {
            ciphertext2 = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        ciphertext1.Should().NotBeEquivalentTo(ciphertext2);
    }

    [Fact]
    public void Encrypt_ProducesNonZeroCiphertext()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var plaintext = new byte[64]; // All zeros

        byte[] ciphertext;
        using (var encryptor = salsa.CreateEncryptor(key, iv))
        {
            ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        // Ciphertext should not be all zeros (keystream XOR zeros = keystream)
        ciphertext.Should().NotBeEquivalentTo(plaintext);
    }

    #endregion

    #region TransformBlock Tests

    [Fact]
    public void TransformBlock_ProcessesCorrectly()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = GenerateRandomBytes(128);
        var output = new byte[128];

        using var encryptor = salsa.CreateEncryptor(key, iv);
        var bytesTransformed = encryptor.TransformBlock(input, 0, input.Length, output, 0);

        bytesTransformed.Should().Be(128);
        output.Should().NotBeEquivalentTo(input);
    }

    [Fact]
    public void TransformBlock_WithOffset_ProcessesCorrectly()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = GenerateRandomBytes(128);
        var output = new byte[128];

        using var encryptor = salsa.CreateEncryptor(key, iv);
        // Transform only last 64 bytes
        var bytesTransformed = encryptor.TransformBlock(input, 64, 64, output, 64);

        bytesTransformed.Should().Be(64);
    }

    [Fact]
    public void TransformBlock_MultipleBlocks_ProcessesCorrectly()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = GenerateRandomBytes(256); // 4 blocks
        var output = new byte[256];

        using var encryptor = salsa.CreateEncryptor(key, iv);
        var bytesTransformed = encryptor.TransformBlock(input, 0, input.Length, output, 0);

        bytesTransformed.Should().Be(256);
    }

    [Fact]
    public void TransformFinalBlock_WithSmallInput_ProcessesCorrectly()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = GenerateRandomBytes(13); // Not a multiple of 64

        using var encryptor = salsa.CreateEncryptor(key, iv);
        var output = encryptor.TransformFinalBlock(input, 0, input.Length);

        output.Should().HaveCount(13);
    }

    #endregion

    #region ICryptoTransform Properties Tests

    [Fact]
    public void CryptoTransform_CanTransformMultipleBlocks_IsTrue()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);

        using var transform = salsa.CreateEncryptor(key, iv);

        transform.CanTransformMultipleBlocks.Should().BeTrue();
    }

    [Fact]
    public void CryptoTransform_CanReuseTransform_IsFalse()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);

        using var transform = salsa.CreateEncryptor(key, iv);

        transform.CanReuseTransform.Should().BeFalse();
    }

    [Fact]
    public void CryptoTransform_BlockSizes_Are64()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);

        using var transform = salsa.CreateEncryptor(key, iv);

        transform.InputBlockSize.Should().Be(64);
        transform.OutputBlockSize.Should().Be(64);
    }

    #endregion

    #region GenerateKey/GenerateIV Tests

    [Fact]
    public void GenerateKey_ProducesKeyOfCorrectSize()
    {
        using var salsa = new Salsa20();
        salsa.KeySize = 256;

        salsa.GenerateKey();

        salsa.Key.Should().HaveCount(32);
    }

    [Fact]
    public void GenerateIV_ProducesIVOfCorrectSize()
    {
        using var salsa = new Salsa20();

        salsa.GenerateIV();

        salsa.IV.Should().HaveCount(8);
    }

    [Fact]
    public void GenerateKey_ProducesDifferentKeysEachTime()
    {
        using var salsa = new Salsa20();

        salsa.GenerateKey();
        var key1 = (byte[])salsa.Key.Clone();

        salsa.GenerateKey();
        var key2 = salsa.Key;

        key1.Should().NotBeEquivalentTo(key2);
    }

    #endregion

    #region Algorithm Properties Tests

    [Fact]
    public void LegalBlockSizes_Contains512()
    {
        using var salsa = new Salsa20();

        salsa.LegalBlockSizes.Should().ContainSingle()
            .Which.Should().Match<KeySizes>(ks =>
                ks.MinSize == 512 && ks.MaxSize == 512);
    }

    [Fact]
    public void LegalKeySizes_Contains128And256()
    {
        using var salsa = new Salsa20();

        var keySizes = salsa.LegalKeySizes[0];
        keySizes.MinSize.Should().Be(128);
        keySizes.MaxSize.Should().Be(256);
        keySizes.SkipSize.Should().Be(128);
    }

    [Fact]
    public void DefaultKeySize_Is256()
    {
        using var salsa = new Salsa20();

        salsa.KeySize.Should().Be(256);
    }

    [Fact]
    public void DefaultBlockSize_Is512()
    {
        using var salsa = new Salsa20();

        salsa.BlockSize.Should().Be(512);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void TransformBlock_AfterDispose_ThrowsObjectDisposedException()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = new byte[64];
        var output = new byte[64];

        var transform = salsa.CreateEncryptor(key, iv);
        transform.Dispose();

        var act = () => transform.TransformBlock(input, 0, 64, output, 0);

        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Encrypt_EmptyInput_ReturnsEmptyOutput()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = Array.Empty<byte>();

        using var encryptor = salsa.CreateEncryptor(key, iv);
        var output = encryptor.TransformFinalBlock(input, 0, 0);

        output.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_LargeInput_ProcessesCorrectly()
    {
        using var salsa = new Salsa20();
        var key = GenerateRandomBytes(32);
        var iv = GenerateRandomBytes(8);
        var input = GenerateRandomBytes(10000); // Large input

        byte[] ciphertext;
        using (var encryptor = salsa.CreateEncryptor(key, iv))
        {
            ciphertext = encryptor.TransformFinalBlock(input, 0, input.Length);
        }

        byte[] decrypted;
        using (var decryptor = salsa.CreateDecryptor(key, iv))
        {
            decrypted = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        decrypted.Should().BeEquivalentTo(input);
    }

    #endregion

    #region Helper Methods

    private static byte[] GenerateRandomBytes(int count)
    {
        var bytes = new byte[count];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    #endregion
}
