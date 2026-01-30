using System;
using System.Threading.Tasks;


#nullable enable

namespace BuildBackup.Interfaces;

/// <summary>
/// Service for managing TACT encryption keys.
/// </summary>
public interface IKeyService
{
    /// <summary>
    /// Gets an encryption key by its lookup name.
    /// </summary>
    /// <param name="keyName">The key lookup value.</param>
    /// <returns>The key bytes, or null if not found.</returns>
    byte[]? GetKey(ulong keyName);

    /// <summary>
    /// Gets the Salsa20 cipher instance.
    /// </summary>
    Salsa20 SalsaInstance { get; }

    /// <summary>
    /// Loads encryption keys from cache or remote source.
    /// </summary>
    Task LoadKeysAsync();

    /// <summary>
    /// Checks if a key is available.
    /// </summary>
    /// <param name="keyName">The key lookup value.</param>
    /// <returns>True if the key is available.</returns>
    bool IsKeyAvailable(ulong keyName);
}
