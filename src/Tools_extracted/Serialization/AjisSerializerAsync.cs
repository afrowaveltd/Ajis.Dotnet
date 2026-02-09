using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Afrowave.AJIS;

/// <summary>
/// Provides asynchronous serialization extension methods.
/// </summary>
public static class AjisSerializerAsync
{
    /// <summary>
    /// Asynchronously serializes a value to a stream.
    /// </summary>
    public static async Task SerializeToStreamAsync(
        this AjisValue value,
        Stream stream,
        AjisSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= AjisSerializerOptions.Json; // Default to compact JSON
        var json = AjisSerializer.Serialize(value, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously serializes a value to a file.
    /// </summary>
    public static async Task SerializeToFileAsync(
        this AjisValue value,
        string filePath,
        AjisSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);
        await value.SerializeToStreamAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }
}
