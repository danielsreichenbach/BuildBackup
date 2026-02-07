using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuildBackup.Interfaces;

#nullable enable

namespace BuildBackup.Services;

/// <summary>
/// Service for parsing CASC (Content Addressable Storage Container) files.
/// Handles encoding, root, install, download, and patch files.
/// </summary>
public sealed class CascParserService : ICascParserService
{
    private readonly ICdnClient _cdnClient;

    /// <summary>
    /// Creates a new CASC parser service.
    /// </summary>
    /// <param name="cdnClient">CDN client for downloading files.</param>
    public CascParserService(ICdnClient cdnClient)
    {
        _cdnClient = cdnClient ?? throw new ArgumentNullException(nameof(cdnClient));
    }

    public async Task<EncodingFile> GetEncodingAsync(string cdnPath, string hash, EncodingParseOptions? options = null, CancellationToken ct = default)
    {
        options ??= new EncodingParseOptions();
        var encoding = new EncodingFile();

        byte[] data;
        if (options.IsEncoded)
        {
            var path = $"{cdnPath}/data/{hash[..2]}/{hash[2..4]}/{hash}";
            var content = await _cdnClient.GetAsync(path);

            if (options.ExpectedSize != 0 && options.ExpectedSize != content.Length)
            {
                content = await _cdnClient.GetAsync(path, new CdnGetOptions { ForceRedownload = true });

                if (options.ExpectedSize != content.Length && options.ExpectedSize != 0)
                {
                    throw new Exception($"File corrupt/not fully downloaded! Remove data/{hash[..2]}/{hash[2..4]}/{hash} from cache.");
                }
            }

            data = BLTE.Parse(content);
        }
        else
        {
            if (string.IsNullOrEmpty(options.LocalPath))
                throw new ArgumentException("LocalPath must be set when IsEncoded is false");
            data = File.ReadAllBytes(options.LocalPath);
        }

        using var bin = new BinaryReader(new MemoryStream(data));

        if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "EN")
            throw new Exception("Error while parsing encoding file. Did BLTE header size change?");

        encoding.unk1 = bin.ReadByte();
        encoding.checksumSizeA = bin.ReadByte();
        encoding.checksumSizeB = bin.ReadByte();
        encoding.sizeA = bin.ReadUInt16(true);
        encoding.sizeB = bin.ReadUInt16(true);
        encoding.numEntriesA = bin.ReadUInt32(true);
        encoding.numEntriesB = bin.ReadUInt32(true);
        bin.ReadByte(); // unk
        encoding.stringBlockSize = bin.ReadUInt32(true);

        var headerLength = bin.BaseStream.Position;
        var stringBlockEntries = new List<string>();

        if (options.ParseTableB)
        {
            while ((bin.BaseStream.Position - headerLength) != (long)encoding.stringBlockSize)
            {
                stringBlockEntries.Add(bin.ReadCString());
            }
            encoding.stringBlockEntries = stringBlockEntries.ToArray();
        }
        else
        {
            bin.BaseStream.Position += (long)encoding.stringBlockSize;
        }

        // Table A
        if (options.CheckHeaders)
        {
            encoding.aHeaders = new EncodingHeaderEntry[encoding.numEntriesA];
            for (int i = 0; i < encoding.numEntriesA; i++)
            {
                encoding.aHeaders[i].firstHash = Convert.ToHexString(bin.ReadBytes(16));
                encoding.aHeaders[i].checksum = Convert.ToHexString(bin.ReadBytes(16));
            }
        }
        else
        {
            bin.BaseStream.Position += encoding.numEntriesA * 32;
        }

        var tableAstart = bin.BaseStream.Position;
        var entries = new List<EncodingFileEntry>();

        for (int i = 0; i < encoding.numEntriesA; i++)
        {
            ushort keysCount;
            while ((keysCount = bin.ReadUInt16()) != 0)
            {
                var entry = new EncodingFileEntry
                {
                    keyCount = keysCount,
                    size = bin.ReadUInt32(true),
                    cKey = Convert.ToHexString(bin.ReadBytes(16)),
                    eKeys = new List<string>()
                };

                for (int key = 0; key < entry.keyCount; key++)
                {
                    entry.eKeys.Add(Convert.ToHexString(bin.ReadBytes(16)));
                }

                entries.Add(entry);
            }

            var remaining = 4096 - ((bin.BaseStream.Position - tableAstart) % 4096);
            if (remaining > 0)
                bin.BaseStream.Position += remaining;
        }

        encoding.aEntries = entries.ToArray();

        if (!options.ParseTableB)
            return encoding;

        // Table B
        if (options.CheckHeaders)
        {
            encoding.bHeaders = new EncodingHeaderEntry[encoding.numEntriesB];
            for (int i = 0; i < encoding.numEntriesB; i++)
            {
                encoding.bHeaders[i].firstHash = Convert.ToHexString(bin.ReadBytes(16));
                encoding.bHeaders[i].checksum = Convert.ToHexString(bin.ReadBytes(16));
            }
        }
        else
        {
            bin.BaseStream.Position += encoding.numEntriesB * 32;
        }

        var tableBstart = bin.BaseStream.Position;
        encoding.bEntries = new Dictionary<string, EncodingFileDescEntry>();

        while (bin.BaseStream.Position < tableBstart + 4096 * encoding.numEntriesB)
        {
            var remaining = 4096 - (bin.BaseStream.Position - tableBstart) % 4096;

            if (remaining < 25)
            {
                bin.BaseStream.Position += remaining;
                continue;
            }

            var key = Convert.ToHexString(bin.ReadBytes(16));

            var entry = new EncodingFileDescEntry
            {
                stringIndex = bin.ReadUInt32(true),
                compressedSize = bin.ReadUInt40(true)
            };

            if (entry.stringIndex == uint.MaxValue)
                break;

            encoding.bEntries.Add(key, entry);
        }

        // Go to the end until we hit a non-NUL byte
        while (bin.BaseStream.Position < bin.BaseStream.Length)
        {
            if (bin.ReadByte() != 0)
                break;
        }

        bin.BaseStream.Position -= 1;
        var eespecSize = bin.BaseStream.Length - bin.BaseStream.Position;
        encoding.encodingESpec = new string(bin.ReadChars((int)eespecSize));

        return encoding;
    }

    public async Task<RootFile> GetRootAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default)
    {
        var root = new RootFile
        {
            entriesLookup = new MultiDictionary<ulong, RootEntry>(),
            entriesFDID = new MultiDictionary<uint, RootEntry>()
        };

        var path = $"{cdnPath}/data/{hash[..2]}/{hash[2..4]}/{hash}";
        var content = await _cdnClient.GetAsync(path);

        if (!parse)
            return root;

        var namedCount = 0;
        var unnamedCount = 0;
        uint totalFiles = 0;
        uint namedFiles = 0;
        var newRoot = false;

        uint dfHeaderSize = 0;
        uint dfVersion = 0;

        using var bin = new BinaryReader(new MemoryStream(BLTE.Parse(content)));

        var header = bin.ReadUInt32();

        if (header == 1296454484) // 'MFST'
        {
            totalFiles = bin.ReadUInt32();
            namedFiles = bin.ReadUInt32();

            if (namedFiles == 1 || namedFiles == 2)
            {
                // Post 10.1.7
                dfHeaderSize = totalFiles;
                dfVersion = namedFiles;

                if (dfVersion == 1 || dfVersion == 2)
                {
                    totalFiles = bin.ReadUInt32();
                    namedFiles = bin.ReadUInt32();
                }

                bin.BaseStream.Position = dfHeaderSize;
            }

            newRoot = true;
        }
        else
        {
            bin.BaseStream.Position = 0;
        }

        while (bin.BaseStream.Position < bin.BaseStream.Length)
        {
            uint count;
            ContentFlags contentFlags;
            LocaleFlags localeFlags;

            if (dfVersion == 2)
            {
                count = bin.ReadUInt32();
                localeFlags = (LocaleFlags)bin.ReadUInt32();
                var unkFlags = bin.ReadUInt32();
                contentFlags = (ContentFlags)bin.ReadUInt32();
                var unkByte = bin.ReadByte();
            }
            else
            {
                count = bin.ReadUInt32();
                contentFlags = (ContentFlags)bin.ReadUInt32();
                localeFlags = (LocaleFlags)bin.ReadUInt32();
            }

            var entries = new RootEntry[count];
            var filedataIds = new int[count];

            var fileDataIndex = 0;
            for (var i = 0; i < count; ++i)
            {
                entries[i].localeFlags = localeFlags;
                entries[i].contentFlags = contentFlags;

                filedataIds[i] = fileDataIndex + bin.ReadInt32();
                entries[i].fileDataID = (uint)filedataIds[i];
                fileDataIndex = filedataIds[i] + 1;
            }

            if (!newRoot)
            {
                for (var i = 0; i < count; ++i)
                {
                    entries[i].md5 = bin.ReadBytes(16);
                    entries[i].lookup = bin.ReadUInt64();
                    root.entriesLookup.Add(entries[i].lookup, entries[i]);
                    root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                }
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    entries[i].md5 = bin.ReadBytes(16);
                }

                for (var i = 0; i < count; ++i)
                {
                    if (contentFlags.HasFlag(ContentFlags.NoNames))
                    {
                        entries[i].lookup = 0;
                        unnamedCount++;
                    }
                    else
                    {
                        entries[i].lookup = bin.ReadUInt64();
                        root.entriesLookup.Add(entries[i].lookup, entries[i]);
                        namedCount++;
                    }

                    root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                }
            }
        }

        if (namedFiles > 0 && namedFiles != namedCount)
            throw new Exception($"Didn't read correct amount of named files! Read {namedCount} but expected {namedFiles}");

        if (totalFiles > 0 && totalFiles != (namedCount + unnamedCount))
            throw new Exception($"Didn't read correct amount of total files! Read {namedCount + unnamedCount} but expected {totalFiles}");

        return root;
    }

    public async Task<InstallFile> GetInstallAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default)
    {
        var install = new InstallFile();

        var path = $"{cdnPath}/data/{hash[..2]}/{hash[2..4]}/{hash}";
        var content = await _cdnClient.GetAsync(path);

        if (!parse)
            return install;

        using var bin = new BinaryReader(new MemoryStream(BLTE.Parse(content)));

        if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN")
            throw new Exception("Error while parsing install file. Did BLTE header size change?");

        bin.ReadByte();

        install.hashSize = bin.ReadByte();
        if (install.hashSize != 16)
            throw new Exception("Unsupported install hash size!");

        install.numTags = bin.ReadUInt16(true);
        install.numEntries = bin.ReadUInt32(true);

        int bytesPerTag = ((int)install.numEntries + 7) / 8;

        install.tags = new InstallTagEntry[install.numTags];

        for (var i = 0; i < install.numTags; i++)
        {
            install.tags[i].name = bin.ReadCString();
            install.tags[i].type = bin.ReadUInt16(true);

            var filebits = bin.ReadBytes(bytesPerTag);

            for (int j = 0; j < bytesPerTag; j++)
                filebits[j] = (byte)((filebits[j] * 0x0202020202 & 0x010884422010) % 1023);

            install.tags[i].files = new BitArray(filebits);
        }

        install.entries = new InstallFileEntry[install.numEntries];

        for (var i = 0; i < install.numEntries; i++)
        {
            install.entries[i].name = bin.ReadCString();
            install.entries[i].contentHash = bin.ReadBytes(install.hashSize);
            install.entries[i].size = bin.ReadUInt32(true);
            install.entries[i].tags = new List<string>();
            for (var j = 0; j < install.numTags; j++)
            {
                if (install.tags[j].files[i])
                {
                    install.entries[i].tags.Add($"{install.tags[j].type}={install.tags[j].name}");
                }
            }
        }

        return install;
    }

    public async Task<DownloadFile> GetDownloadAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default)
    {
        var download = new DownloadFile();

        var path = $"{cdnPath}/data/{hash[..2]}/{hash[2..4]}/{hash}";
        var content = await _cdnClient.GetAsync(path);

        if (!parse)
            return download;

        using var bin = new BinaryReader(new MemoryStream(BLTE.Parse(content)));

        if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL")
            throw new Exception("Error while parsing download file. Did BLTE header size change?");

        download.version = bin.ReadByte();
        download.hashSizeEKey = bin.ReadByte();
        download.hasChecksumInEntry = bin.ReadBoolean();
        download.numEntries = bin.ReadUInt32(true);
        download.numTags = bin.ReadUInt16(true);
        download.flagSize = bin.ReadByte();

        download.entries = new DownloadEntry[download.numEntries];
        for (int i = 0; i < download.numEntries; i++)
        {
            download.entries[i].eKey = Convert.ToHexString(bin.ReadBytes(download.hashSizeEKey));
            download.entries[i].size = bin.ReadUInt40(true);
            download.entries[i].priority = bin.ReadByte();

            if (download.hasChecksumInEntry)
            {
                download.entries[i].checksum = bin.ReadUInt32(true);
            }

            if (download.flagSize == 1)
            {
                download.entries[i].flags = bin.ReadByte();
            }
            else
            {
                throw new Exception("Unexpected download flag size");
            }
        }

        return download;
    }

    public async Task<PatchFile> GetPatchAsync(string cdnPath, string hash, bool parse = false, CancellationToken ct = default)
    {
        var patchFile = new PatchFile();

        var path = $"{cdnPath}/patch/{hash[..2]}/{hash[2..4]}/{hash}";
        var content = await _cdnClient.GetAsync(path);

        if (!parse)
            return patchFile;

        using var bin = new BinaryReader(new MemoryStream(content));

        if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "PA")
            throw new Exception("Error while parsing patch file!");

        patchFile.version = bin.ReadByte();
        patchFile.fileKeySize = bin.ReadByte();
        patchFile.sizeB = bin.ReadByte();
        patchFile.patchKeySize = bin.ReadByte();
        patchFile.blockSizeBits = bin.ReadByte();
        patchFile.blockCount = bin.ReadUInt16(true);
        patchFile.flags = bin.ReadByte();
        patchFile.encodingContentKey = bin.ReadBytes(16);
        patchFile.encodingEncodingKey = bin.ReadBytes(16);
        patchFile.decodedSize = bin.ReadUInt32(true);
        patchFile.encodedSize = bin.ReadUInt32(true);
        patchFile.especLength = bin.ReadByte();
        patchFile.encodingSpec = new string(bin.ReadChars(patchFile.especLength));

        patchFile.blocks = new PatchBlock[patchFile.blockCount];
        for (var i = 0; i < patchFile.blockCount; i++)
        {
            patchFile.blocks[i].lastFileContentKey = bin.ReadBytes(patchFile.fileKeySize);
            patchFile.blocks[i].blockMD5 = bin.ReadBytes(16);
            patchFile.blocks[i].blockOffset = bin.ReadUInt32(true);

            var prevPos = bin.BaseStream.Position;

            var files = new List<BlockFile>();

            bin.BaseStream.Position = patchFile.blocks[i].blockOffset;
            while (bin.BaseStream.Position <= patchFile.blocks[i].blockOffset + 0x10000)
            {
                var blockFile = new BlockFile
                {
                    numPatches = bin.ReadByte()
                };

                if (blockFile.numPatches == 0)
                    break;

                blockFile.targetFileContentKey = bin.ReadBytes(patchFile.fileKeySize);
                blockFile.decodedSize = bin.ReadUInt40(true);

                blockFile.patches = new FilePatch[blockFile.numPatches];
                for (var j = 0; j < blockFile.numPatches; j++)
                {
                    blockFile.patches[j].sourceFileEncodingKey = bin.ReadBytes(patchFile.fileKeySize);
                    blockFile.patches[j].decodedSize = bin.ReadUInt40(true);
                    blockFile.patches[j].patchEncodingKey = bin.ReadBytes(patchFile.patchKeySize);
                    blockFile.patches[j].patchSize = bin.ReadUInt32(true);
                    blockFile.patches[j].patchIndex = bin.ReadByte();
                }

                files.Add(blockFile);
            }

            patchFile.blocks[i].files = files.ToArray();

            bin.BaseStream.Position = prevPos;
        }

        return patchFile;
    }
}
