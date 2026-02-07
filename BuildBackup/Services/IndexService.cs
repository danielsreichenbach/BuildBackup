using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildBackup.Interfaces;

namespace BuildBackup.Services;

/// <summary>
/// Service for parsing and managing archive index files.
/// Index files map encoding keys to archive locations for efficient file retrieval.
/// </summary>
public sealed class IndexService : IIndexService
{
    // Index file format constants
    private const int IndexBlockSize = 4096;
    private const int IndexEntriesPerBlock = 170;
    private const int IndexHashSize = 16;
    private static readonly string IndexEmptyHash = new string('0', 32);

    private readonly ICdnClient _cdnClient;

    /// <summary>
    /// Creates a new index service.
    /// </summary>
    /// <param name="cdnClient">CDN client for downloading index files.</param>
    public IndexService(ICdnClient cdnClient)
    {
        _cdnClient = cdnClient ?? throw new ArgumentNullException(nameof(cdnClient));
    }

    public async Task<IReadOnlyDictionary<string, IndexEntry>> ParseIndexAsync(
        string cdnPath,
        string hash,
        string folder = "data",
        CancellationToken ct = default)
    {
        var path = $"{cdnPath}/{folder}/{hash[..2]}/{hash[2..4]}/{hash}.index";
        var indexContent = await _cdnClient.GetAsync(path);

        var returnDict = new Dictionary<string, IndexEntry>();

        using var ms = new MemoryStream(indexContent);
        using var bin = new BinaryReader(ms);

        bin.BaseStream.Position = bin.BaseStream.Length - 28;

        var footer = new IndexFooter
        {
            tocHash = bin.ReadBytes(8),
            version = bin.ReadByte(),
            unk0 = bin.ReadByte(),
            unk1 = bin.ReadByte(),
            blockSizeKB = bin.ReadByte(),
            offsetBytes = bin.ReadByte(),
            sizeBytes = bin.ReadByte(),
            keySizeInBytes = bin.ReadByte(),
            checksumSize = bin.ReadByte(),
            numElements = bin.ReadUInt32()
        };

        footer.footerChecksum = bin.ReadBytes(footer.checksumSize);

        // Read numElements as BE if it is wrong as LE
        if ((footer.numElements & 0xff000000) != 0)
        {
            bin.BaseStream.Position -= footer.checksumSize + 4;
            footer.numElements = bin.ReadUInt32(true);
        }

        bin.BaseStream.Position = 0;

        var indexBlockSize = 1024 * footer.blockSizeKB;
        var recordSize = footer.keySizeInBytes + footer.sizeBytes + footer.offsetBytes;
        var recordsPerBlock = indexBlockSize / recordSize;
        var recordsRead = 0;

        while (recordsRead != footer.numElements)
        {
            var blockRecordsRead = 0;

            for (var blockIndex = 0; blockIndex < recordsPerBlock && recordsRead < footer.numElements; blockIndex++, recordsRead++)
            {
                var headerHash = Convert.ToHexString(bin.ReadBytes(footer.keySizeInBytes));
                var entry = new IndexEntry();

                if (footer.sizeBytes == 4)
                {
                    entry.size = bin.ReadUInt32(true);
                }
                else
                {
                    throw new NotImplementedException("Index size reading other than 4 is not implemented!");
                }

                if (footer.offsetBytes == 4)
                {
                    // Archive index
                    entry.offset = bin.ReadUInt32(true);
                }
                else if (footer.offsetBytes == 6)
                {
                    // Group index
                    throw new NotImplementedException("Group index reading is not implemented!");
                }
                else if (footer.offsetBytes == 0)
                {
                    // File index - no offset field
                }
                else
                {
                    throw new NotImplementedException("Offset size reading other than 4/6/0 is not implemented!");
                }

                returnDict.Add(headerHash, entry);

                blockRecordsRead++;
            }

            bin.ReadBytes(indexBlockSize - (blockRecordsRead * recordSize));
        }

        return returnDict;
    }

    public async Task PopulateIndexCacheAsync(
        IIndexCache cache,
        string cdnPath,
        string[] archives,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[INDEX] Starting parallel download of {archives.Length} archive indexes from {cdnPath}");

        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);

        for (var i = 0; i < archives.Length; i++)
        {
            var archiveIndex = (short)i;
            var archive = archives[i];

            await semaphore.WaitAsync(ct);

            var task = Task.Run(async () =>
            {
                try
                {
                    var path = $"{cdnPath}/data/{archive[..2]}/{archive[2..4]}/{archive}.index";
                    var indexContent = await _cdnClient.GetAsync(path);

                    foreach (var (hash, size, offset) in ParseIndexEntries(indexContent))
                    {
                        var entry = new IndexEntry
                        {
                            index = archiveIndex,
                            size = size,
                            offset = offset
                        };

                        if (!cache.TryAddDataEntry(hash, entry))
                        {
                            Console.WriteLine($"Duplicate index entry for {hash} (index: {archive}, size: {entry.size}, offset: {entry.offset})");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error retrieving index: {e.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async Task PopulatePatchIndexCacheAsync(
        IIndexCache cache,
        string cdnPath,
        string[] archives,
        CancellationToken ct = default)
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);

        for (var i = 0; i < archives.Length; i++)
        {
            var archiveIndex = (short)i;
            var archive = archives[i];

            await semaphore.WaitAsync(ct);

            var task = Task.Run(async () =>
            {
                try
                {
                    var path = $"{cdnPath}/patch/{archive[..2]}/{archive[2..4]}/{archive}.index";
                    var indexContent = await _cdnClient.GetAsync(path);

                    foreach (var (hash, size, offset) in ParseIndexEntries(indexContent))
                    {
                        var entry = new IndexEntry
                        {
                            index = archiveIndex,
                            size = size,
                            offset = offset
                        };

                        if (!cache.TryAddPatchEntry(hash, entry))
                        {
                            Console.WriteLine($"Duplicate patch index entry for {hash} (index: {archive}, size: {entry.size}, offset: {entry.offset})");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to retrieve patch index: {e.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyDictionary<string, IndexEntry>> ParseFileIndexAsync(
        string cdnPath,
        string hash,
        CancellationToken ct = default)
    {
        return await ParseIndexAsync(cdnPath, hash, "data", ct);
    }

    /// <summary>
    /// Parses index file content and yields entries.
    /// Index files use 4096-byte blocks with 170 entries each (24 bytes per entry + 16-byte footer).
    /// </summary>
    private static IEnumerable<(string Hash, uint Size, uint Offset)> ParseIndexEntries(byte[] indexContent)
    {
        using var bin = new BinaryReader(new MemoryStream(indexContent));
        var blockCount = indexContent.Length / IndexBlockSize;

        for (var block = 0; block < blockCount; block++)
        {
            for (var entry = 0; entry < IndexEntriesPerBlock; entry++)
            {
                var hash = Convert.ToHexString(bin.ReadBytes(IndexHashSize));
                var size = bin.ReadUInt32(true);
                var offset = bin.ReadUInt32(true);

                if (hash != IndexEmptyHash)
                {
                    yield return (hash, size, offset);
                }
            }
            bin.ReadBytes(IndexHashSize); // Skip block footer/checksum
        }
    }
}
