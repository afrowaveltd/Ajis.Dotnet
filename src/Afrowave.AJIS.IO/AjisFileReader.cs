#nullable enable

using System.Runtime.CompilerServices;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Reads AJIS segments from a file with memory-bounded streaming.
/// </summary>
/// <remarks>
/// <para>
/// AjisFileReader enables processing arbitrarily large AJIS files without loading them
/// entirely into memory. It wraps the file and provides streaming access to segments.
/// </para>
/// <para>
/// This class is optimized for sequential reading from AJIS array-based files.
/// </para>
/// </remarks>
public sealed class AjisFileReader : IAsyncDisposable
{
    private readonly string _filePath;
    private FileStream? _fileStream;
    private bool _disposed;
    private long _currentPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="AjisFileReader"/> class.
    /// </summary>
    /// <param name="filePath">Path to the AJIS file to read.</param>
    /// <exception cref="ArgumentNullException">Thrown if filePath is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    public AjisFileReader(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"AJIS file not found: {filePath}", filePath);

        _filePath = filePath;
        _currentPosition = 0;
    }

    /// <summary>
    /// Gets the full path to the file being read.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Gets the total size of the file in bytes.
    /// </summary>
    public long FileSize
    {
        get
        {
            ThrowIfDisposed();
            var info = new FileInfo(_filePath);
            return info.Length;
        }
    }

    /// <summary>
    /// Gets the current read position in the file.
    /// </summary>
    public long CurrentPosition
    {
        get
        {
            ThrowIfDisposed();
            return _currentPosition;
        }
    }

    /// <summary>
    /// Gets whether the reader has reached the end of the file.
    /// </summary>
    public bool IsAtEnd
    {
        get
        {
            ThrowIfDisposed();
            return _currentPosition >= FileSize;
        }
    }

    /// <summary>
    /// Reads file content as a byte stream for AJIS parsing.
    /// </summary>
    /// <returns>The file stream (caller must dispose).</returns>
    public FileStream OpenAsStream()
    {
        ThrowIfDisposed();
        _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return _fileStream;
    }

    /// <summary>
    /// Seeks to a specific byte offset in the file and returns the offset.
    /// </summary>
    /// <param name="byteOffset">The byte offset to seek to.</param>
    /// <returns>The actual position after seeking.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if offset is negative or beyond file size.</exception>
    public long Seek(long byteOffset)
    {
        ThrowIfDisposed();

        if (byteOffset < 0 || byteOffset > FileSize)
            throw new ArgumentOutOfRangeException(nameof(byteOffset), $"Offset must be between 0 and {FileSize}");

        _fileStream ??= new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096);
        _fileStream.Seek(byteOffset, SeekOrigin.Begin);
        _currentPosition = byteOffset;

        return _currentPosition;
    }

    /// <summary>
    /// Resets the reader to the beginning of the file.
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        Seek(0);
    }

    /// <summary>
    /// Closes the underlying file stream and resources.
    /// </summary>
    public void Close()
    {
        _fileStream?.Close();
        _fileStream = null;
    }

    /// <summary>
    /// Disposes the reader and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_fileStream != null)
        {
            await _fileStream.DisposeAsync();
            _fileStream = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws if the reader has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AjisFileReader));
    }
}
