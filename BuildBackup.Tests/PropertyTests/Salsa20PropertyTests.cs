using System;
using System.Security.Cryptography;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace BuildBackup.Tests.PropertyTests;

/// <summary>
/// Property-based tests for Salsa20 cipher.
/// These tests verify invariants that should hold for any valid input.
/// </summary>
public class Salsa20PropertyTests
{
    /// <summary>
    /// Encryption followed by decryption with the same key and IV
    /// must return the original plaintext.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EncryptThenDecrypt_ReturnsOriginalData()
    {
        var keyArb = Arb.From(Gen128Or256BitKey());
        var ivArb = Arb.From(Gen8ByteIV());
        var plaintextArb = Arb.From(GenPlaintext(1, 256));

        return Prop.ForAll(keyArb, ivArb, plaintextArb, (key, iv, plaintext) =>
        {
            using var salsa = new Salsa20();

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

            return decrypted.Length == plaintext.Length &&
                   AreEqual(decrypted, plaintext);
        });
    }

    /// <summary>
    /// Ciphertext length must equal plaintext length (stream cipher property).
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CiphertextLength_EqualsPaintextLength()
    {
        var keyArb = Arb.From(Gen256BitKey());
        var ivArb = Arb.From(Gen8ByteIV());
        var plaintextArb = Arb.From(GenPlaintext(1, 1000));

        return Prop.ForAll(keyArb, ivArb, plaintextArb, (key, iv, plaintext) =>
        {
            using var salsa = new Salsa20();

            byte[] ciphertext;
            using (var encryptor = salsa.CreateEncryptor(key, iv))
            {
                ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
            }

            return ciphertext.Length == plaintext.Length;
        });
    }

    /// <summary>
    /// Different keys produce different ciphertexts (with high probability).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property DifferentKeys_ProduceDifferentCiphertexts()
    {
        var keyPairArb = Arb.From(GenKeyPair256());
        var ivArb = Arb.From(Gen8ByteIV());
        var plaintextArb = Arb.From(GenPlaintext(16, 64));

        return Prop.ForAll(keyPairArb, ivArb, plaintextArb, (keyPair, iv, plaintext) =>
        {
            var (key1, key2) = keyPair;

            // Skip if keys happen to be identical
            if (AreEqual(key1, key2))
                return true;

            using var salsa = new Salsa20();

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

            return !AreEqual(ciphertext1, ciphertext2);
        });
    }

    /// <summary>
    /// Different IVs produce different ciphertexts (with high probability).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property DifferentIVs_ProduceDifferentCiphertexts()
    {
        var keyArb = Arb.From(Gen256BitKey());
        var ivPairArb = Arb.From(GenIVPair());
        var plaintextArb = Arb.From(GenPlaintext(16, 64));

        return Prop.ForAll(keyArb, ivPairArb, plaintextArb, (key, ivPair, plaintext) =>
        {
            var (iv1, iv2) = ivPair;

            // Skip if IVs happen to be identical
            if (AreEqual(iv1, iv2))
                return true;

            using var salsa = new Salsa20();

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

            return !AreEqual(ciphertext1, ciphertext2);
        });
    }

    /// <summary>
    /// Encrypting zeros produces the raw keystream (XOR identity).
    /// Keystream should not be all zeros.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property EncryptZeros_ProducesNonZeroKeystream()
    {
        var keyArb = Arb.From(Gen256BitKey());
        var ivArb = Arb.From(Gen8ByteIV());
        var zerosArb = Arb.From(Gen.Choose(16, 128).Select(n => new byte[n]));

        return Prop.ForAll(keyArb, ivArb, zerosArb, (key, iv, zeros) =>
        {
            using var salsa = new Salsa20();

            byte[] keystream;
            using (var encryptor = salsa.CreateEncryptor(key, iv))
            {
                keystream = encryptor.TransformFinalBlock(zeros, 0, zeros.Length);
            }

            // Keystream should not be all zeros (astronomically unlikely with valid cipher)
            return !IsAllZeros(keystream);
        });
    }

    /// <summary>
    /// All supported round counts produce valid encryption/decryption.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property AllRoundCounts_ProduceValidCipher()
    {
        var keyArb = Arb.From(Gen256BitKey());
        var ivArb = Arb.From(Gen8ByteIV());
        var plaintextArb = Arb.From(GenPlaintext(16, 256));
        var roundsArb = Arb.From(Gen.Elements(8, 12, 20));

        // Combine into tuple to reduce arity
        var combinedArb = Arb.From(
            from key in Gen256BitKey()
            from iv in Gen8ByteIV()
            from plaintext in GenPlaintext(16, 256)
            from rounds in Gen.Elements(8, 12, 20)
            select (key, iv, plaintext, rounds)
        );

        return Prop.ForAll(combinedArb, tuple =>
        {
            var (key, iv, plaintext, rounds) = tuple;

            using var salsa = new Salsa20 { Rounds = rounds };

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

            return AreEqual(plaintext, decrypted);
        });
    }

    /// <summary>
    /// Both 128-bit and 256-bit keys work correctly.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property BothKeySizes_WorkCorrectly()
    {
        var keyArb = Arb.From(Gen128Or256BitKey());
        var ivArb = Arb.From(Gen8ByteIV());
        var plaintextArb = Arb.From(GenPlaintext(16, 128));

        return Prop.ForAll(keyArb, ivArb, plaintextArb, (key, iv, plaintext) =>
        {
            using var salsa = new Salsa20();

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

            return AreEqual(plaintext, decrypted);
        });
    }

    #region Generators

    private static Gen<byte[]> Gen128BitKey()
    {
        return Gen.ArrayOf(16, Arb.Default.Byte().Generator);
    }

    private static Gen<byte[]> Gen256BitKey()
    {
        return Gen.ArrayOf(32, Arb.Default.Byte().Generator);
    }

    private static Gen<byte[]> Gen128Or256BitKey()
    {
        return Gen.OneOf(Gen128BitKey(), Gen256BitKey());
    }

    private static Gen<byte[]> Gen8ByteIV()
    {
        return Gen.ArrayOf(8, Arb.Default.Byte().Generator);
    }

    private static Gen<byte[]> GenPlaintext(int minSize, int maxSize)
    {
        return Gen.Choose(minSize, maxSize)
            .SelectMany(size => Gen.ArrayOf(size, Arb.Default.Byte().Generator));
    }

    private static Gen<(byte[], byte[])> GenKeyPair256()
    {
        return from key1 in Gen256BitKey()
               from key2 in Gen256BitKey()
               select (key1, key2);
    }

    private static Gen<(byte[], byte[])> GenIVPair()
    {
        return from iv1 in Gen8ByteIV()
               from iv2 in Gen8ByteIV()
               select (iv1, iv2);
    }

    #endregion

    #region Helpers

    private static bool AreEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    private static bool IsAllZeros(byte[] data)
    {
        foreach (var b in data)
        {
            if (b != 0)
                return false;
        }
        return true;
    }

    #endregion
}
