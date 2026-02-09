#nullable enable

namespace Afrowave.AJIS.IO;

/// <summary>
/// Writes AJIS content to a file with streaming support.
/// </summary>
/// <remarks>
/// <para>
/// AjisFileWriter enables writing AJIS documents to files without materializing the entire
/// document in memory. It wraps file I/O operations and provides a simple, safe interface.
/// </para>
/// <para>
/// Supports both creating new files and appending to existing files.
/// </para>
/// </remarks>
public sealed class AjisFileWriter : IAsyncDisposable
{
    private readonly string _filePath;
    private FileStream? _fileStream;
    private StreamWriter? _writer;
    private bool _disposed;
    private bool _finalized;

    /// <summary>
    /// Initializes a new instance of the <see cref="AjisFileWriter"/> class.
    /// </summary>
    /// <param name="filePath">Path to the AJIS file to write to.</param>
    /// <param name="mode">File mode (Create, Append, etc.).</param>
    /// <exception cref="ArgumentNullException">Thrown if filePath is null.</exception>
    public AjisFileWriter(string filePath, FileMode mode = FileMode.Create)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        _filePath = filePath;

        // Create directory if needed
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        _fileStream = new FileStream(filePath, mode, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        _writer = new StreamWriter(_fileStream, System.Text.Encoding.UTF8, bufferSize: 4096);
    }

    /// <summary>
    /// Gets the full path to the file being written to.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Gets whether the writer has been finalized (file closed).
    /// </summary>
    public bool IsFinalized => _finalized;

    /// <summary>
    /// Writes text content to the file.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if writer is disposed.</exception>
    public void Write(string content)
    {
        ThrowIfDisposed();
        if (_finalized)
            throw new InvalidOperationException("Writer has been finalized (file is closed).");

        _writer?.Write(content);
    }

    /// <summary>
    /// Writes text content to the file asynchronously.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteAsync(string content, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_finalized)
            throw new InvalidOperationException("Writer has been finalized (file is closed).");

        if (_writer != null)
            await _writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Writes a line of text to the file.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if writer is disposed.</exception>
    public void WriteLine(string content)
    {
        ThrowIfDisposed();
        if (_finalized)
            throw new InvalidOperationException("Writer has been finalized (file is closed).");

        _writer?.WriteLine(content);
    }

    /// <summary>
    /// Writes a line of text to the file asynchronously.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteLineAsync(string content, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_finalized)
            throw new InvalidOperationException("Writer has been finalized (file is closed).");

        if (_writer != null)
            await _writer.WriteLineAsync(content.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Flushes buffered content to the file.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if writer is disposed.</exception>
    public void Flush()
    {
        ThrowIfDisposed();
        _writer?.Flush();
    }

    /// <summary>
    /// Flushes buffered content to the file asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_writer != null)
            await _writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Finalizes and closes the file, flushing all pending writes.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if writer is disposed.</exception>
    public async Task FinalizeAsync()
    {
        ThrowIfDisposed();

        if (_finalized)
            return; // Already finalized

        try
        {
            if (_writer != null)
                await _writer.FlushAsync();
        }
        finally
        {
            _finalized = true;
        }
    }

    /// <summary>
    /// Disposes the writer and closes the file.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (!_finalized && _writer != null)
                await _writer.FlushAsync();

            _writer?.Dispose();
            _fileStream?.Dispose();
        }
        finally
        {
            _writer = null;
            _fileStream = null;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws if the writer has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AjisFileWriter));
    }
}
