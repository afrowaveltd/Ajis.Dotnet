using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Afrowave.AJIS;

/// <summary>
/// Provides asynchronous parsing capabilities for AJIS/JSON documents.
/// </summary>
public sealed partial class AjisDocument
{
    /// <summary>
    /// Asynchronously parses AJIS/JSON from a stream.
    /// </summary>
    public static async Task<AjisDocument> ParseAsync(
        Stream stream,
        AjisLexerOptions? lexerOptions = null,
        AjisParserOptions? parserOptions = null,
        CancellationToken cancellationToken = default)
    {
        // Use Utf8Parser for async streaming
        var value = await AjisUtf8Parser.ParseAsync(stream, cancellationToken).ConfigureAwait(false);
        return new AjisDocument(value);
    }

    /// <summary>
    /// Asynchronously parses AJIS/JSON from a file.
    /// </summary>
    public static async Task<AjisDocument> ParseFileAsync(
        string filePath,
        AjisLexerOptions? lexerOptions = null,
        AjisParserOptions? parserOptions = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        return await ParseAsync(stream, lexerOptions, parserOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the document to a stream.
    /// </summary>
    public async Task WriteToAsync(
        Stream stream,
        AjisSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var json = AjisSerializer.Serialize(Root, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes the document to a file.
    /// </summary>
    public async Task WriteToFileAsync(
        string filePath,
        AjisSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);
        await WriteToAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }
}
