using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Afrowave.AJIS.Legacy;

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
        options ??= AjisSerializerOptions.Ajis; // Default to AJIS mode

        // Use AjisUtf8Serializer to produce UTF-8 bytes into a pooled buffer, then write asynchronously
        var bufferWriter = Afrowave.AJIS.Legacy.AjisUtf8Serializer.SerializeToBufferWriter(value, options);
        try
        {
            var mem = bufferWriter.WrittenMemory;
            await stream.WriteAsync(mem, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Afrowave.AJIS.Legacy.AjisUtf8Serializer.ReturnBufferWriter(bufferWriter);
        }
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
