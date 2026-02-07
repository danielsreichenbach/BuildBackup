using System;
using System.Collections.Generic;

namespace BuildBackup.Interfaces;

/// <summary>
/// Thread-safe cache for archive index entries.
/// Provides lookup of encoding keys to archive locations.
/// </summary>
public interface IIndexCache : IDisposable
{
    /// <summary>
    /// Data archive indexes (encoding key -> archive location).
    /// </summary>
    IReadOnlyDictionary<string, IndexEntry> DataIndexes { get; }

    /// <summary>
    /// Patch archive indexes (encoding key -> patch archive location).
    /// </summary>
    IReadOnlyDictionary<string, IndexEntry> PatchIndexes { get; }

    /// <summary>
    /// File indexes for loose files (encoding key -> file location).
    /// </summary>
    IReadOnlyDictionary<string, IndexEntry> FileIndexes { get; }

    /// <summary>
    /// Patch file indexes (encoding key -> patch file location).
    /// </summary>
    IReadOnlyDictionary<string, IndexEntry> PatchFileIndexes { get; }

    /// <summary>
    /// Tries to get a data index entry by encoding key.
    /// </summary>
    /// <param name="hash">Encoding key (hex string).</param>
    /// <param name="entry">The index entry if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGetDataEntry(string hash, out IndexEntry entry);

    /// <summary>
    /// Tries to get a patch index entry by encoding key.
    /// </summary>
    /// <param name="hash">Encoding key (hex string).</param>
    /// <param name="entry">The index entry if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGetPatchEntry(string hash, out IndexEntry entry);

    /// <summary>
    /// Tries to get a file index entry by encoding key.
    /// </summary>
    /// <param name="hash">Encoding key (hex string).</param>
    /// <param name="entry">The index entry if found.</param>
    /// <returns>True if found, false otherwise.</returns>
    bool TryGetFileEntry(string hash, out IndexEntry entry);

    /// <summary>
    /// Adds a data index entry. Thread-safe.
    /// </summary>
    /// <param name="hash">Encoding key (hex string).</param>
    /// <param name="entry">The index entry.</param>
    /// <returns>True if added, false if already exists.</returns>
    bool TryAddDataEntry(string hash, IndexEntry entry);

    /// <summary>
    /// Adds a patch index entry. Thread-safe.
    /// </summary>
    /// <param name="hash">Encoding key (hex string).</param>
    /// <param name="entry">The index entry.</param>
    /// <returns>True if added, false if already exists.</returns>
    bool TryAddPatchEntry(string hash, IndexEntry entry);

    /// <summary>
    /// Sets the file indexes (bulk operation).
    /// </summary>
    /// <param name="indexes">File index dictionary.</param>
    void SetFileIndexes(IReadOnlyDictionary<string, IndexEntry> indexes);

    /// <summary>
    /// Sets the patch file indexes (bulk operation).
    /// </summary>
    /// <param name="indexes">Patch file index dictionary.</param>
    void SetPatchFileIndexes(IReadOnlyDictionary<string, IndexEntry> indexes);

    /// <summary>
    /// Clears all cached indexes.
    /// </summary>
    void Clear();
}
