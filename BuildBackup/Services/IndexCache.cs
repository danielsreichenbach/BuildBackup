using System;
using System.Collections.Generic;
using System.Threading;
using BuildBackup.Interfaces;

namespace BuildBackup.Services;

/// <summary>
/// Thread-safe cache for archive index entries.
/// Uses ReaderWriterLockSlim for concurrent read access with exclusive writes.
/// </summary>
public sealed class IndexCache : IIndexCache
{
    private readonly Dictionary<string, IndexEntry> _dataIndexes = new();
    private readonly Dictionary<string, IndexEntry> _patchIndexes = new();
    private Dictionary<string, IndexEntry> _fileIndexes = new();
    private Dictionary<string, IndexEntry> _patchFileIndexes = new();
    private readonly ReaderWriterLockSlim _dataLock = new();
    private readonly ReaderWriterLockSlim _patchLock = new();
    private readonly object _fileIndexLock = new();
    private readonly object _patchFileIndexLock = new();
    private bool _disposed;

    public IReadOnlyDictionary<string, IndexEntry> DataIndexes
    {
        get
        {
            _dataLock.EnterReadLock();
            try
            {
                return new Dictionary<string, IndexEntry>(_dataIndexes);
            }
            finally
            {
                _dataLock.ExitReadLock();
            }
        }
    }

    public IReadOnlyDictionary<string, IndexEntry> PatchIndexes
    {
        get
        {
            _patchLock.EnterReadLock();
            try
            {
                return new Dictionary<string, IndexEntry>(_patchIndexes);
            }
            finally
            {
                _patchLock.ExitReadLock();
            }
        }
    }

    public IReadOnlyDictionary<string, IndexEntry> FileIndexes
    {
        get
        {
            lock (_fileIndexLock)
            {
                return new Dictionary<string, IndexEntry>(_fileIndexes);
            }
        }
    }

    public IReadOnlyDictionary<string, IndexEntry> PatchFileIndexes
    {
        get
        {
            lock (_patchFileIndexLock)
            {
                return new Dictionary<string, IndexEntry>(_patchFileIndexes);
            }
        }
    }

    public bool TryGetDataEntry(string hash, out IndexEntry entry)
    {
        _dataLock.EnterReadLock();
        try
        {
            return _dataIndexes.TryGetValue(hash, out entry);
        }
        finally
        {
            _dataLock.ExitReadLock();
        }
    }

    public bool TryGetPatchEntry(string hash, out IndexEntry entry)
    {
        _patchLock.EnterReadLock();
        try
        {
            return _patchIndexes.TryGetValue(hash, out entry);
        }
        finally
        {
            _patchLock.ExitReadLock();
        }
    }

    public bool TryGetFileEntry(string hash, out IndexEntry entry)
    {
        lock (_fileIndexLock)
        {
            return _fileIndexes.TryGetValue(hash, out entry);
        }
    }

    public bool TryAddDataEntry(string hash, IndexEntry entry)
    {
        _dataLock.EnterUpgradeableReadLock();
        try
        {
            if (_dataIndexes.ContainsKey(hash))
            {
                return false;
            }

            _dataLock.EnterWriteLock();
            try
            {
                return _dataIndexes.TryAdd(hash, entry);
            }
            finally
            {
                _dataLock.ExitWriteLock();
            }
        }
        finally
        {
            _dataLock.ExitUpgradeableReadLock();
        }
    }

    public bool TryAddPatchEntry(string hash, IndexEntry entry)
    {
        _patchLock.EnterUpgradeableReadLock();
        try
        {
            if (_patchIndexes.ContainsKey(hash))
            {
                return false;
            }

            _patchLock.EnterWriteLock();
            try
            {
                return _patchIndexes.TryAdd(hash, entry);
            }
            finally
            {
                _patchLock.ExitWriteLock();
            }
        }
        finally
        {
            _patchLock.ExitUpgradeableReadLock();
        }
    }

    public void SetFileIndexes(IReadOnlyDictionary<string, IndexEntry> indexes)
    {
        lock (_fileIndexLock)
        {
            _fileIndexes = new Dictionary<string, IndexEntry>(indexes);
        }
    }

    public void SetPatchFileIndexes(IReadOnlyDictionary<string, IndexEntry> indexes)
    {
        lock (_patchFileIndexLock)
        {
            _patchFileIndexes = new Dictionary<string, IndexEntry>(indexes);
        }
    }

    public void Clear()
    {
        _dataLock.EnterWriteLock();
        try
        {
            _dataIndexes.Clear();
        }
        finally
        {
            _dataLock.ExitWriteLock();
        }

        _patchLock.EnterWriteLock();
        try
        {
            _patchIndexes.Clear();
        }
        finally
        {
            _patchLock.ExitWriteLock();
        }

        lock (_fileIndexLock)
        {
            _fileIndexes.Clear();
        }

        lock (_patchFileIndexLock)
        {
            _patchFileIndexes.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Clear();
        _dataLock.Dispose();
        _patchLock.Dispose();
        _disposed = true;
    }
}
