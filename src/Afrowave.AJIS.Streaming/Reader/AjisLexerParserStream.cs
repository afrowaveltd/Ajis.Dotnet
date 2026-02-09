#nullable enable

using Afrowave.AJIS.Streaming.Segments;

namespace Afrowave.AJIS.Streaming.Reader;

/// <summary>
/// Provides streaming segment parsing from AJIS text input.
/// </summary>
public static class AjisLexerParserStream
{
    /// <summary>
    /// Parses AJIS text from a span and returns all segments as a materialized list.
    /// </summary>
    /// <param name="stream">Input stream containing AJIS text.</param>
    /// <param name="bufferSize">Buffer size for stream reading (default: 4096 bytes).</param>
    /// <param name="numberOptions">Numeric parsing options.</param>
    /// <param name="stringOptions">String parsing options.</param>
    /// <param name="commentOptions">Comment parsing options.</param>
    /// <param name="textMode">AJIS text mode (JSON/AJIS/Lex).</param>
    /// <param name="allowTrailingCommas">Allow trailing commas in objects and arrays.</param>
    /// <param name="allowDirectives">Allow AJIS directives.</param>
    /// <param name="preserveStringEscapes">Preserve escape sequences in string slices.</param>
    /// <param name="emitDirectiveSegments">Emit directive segments when encountered.</param>
    /// <param name="emitCommentSegments">Emit comment segments when encountered.</param>
    /// <returns>List of <see cref="AjisSegment"/> objects representing the parsed AJIS.</returns>
    public static IReadOnlyList<AjisSegment> Parse(
        Stream stream,
        int bufferSize = 4096,
        global::Afrowave.AJIS.Core.AjisNumberOptions? numberOptions = null,
        global::Afrowave.AJIS.Core.AjisStringOptions? stringOptions = null,
        global::Afrowave.AJIS.Core.AjisCommentOptions? commentOptions = null,
        global::Afrowave.AJIS.Core.AjisTextMode textMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
        bool allowTrailingCommas = false,
        bool allowDirectives = true,
        bool preserveStringEscapes = false,
        bool emitDirectiveSegments = false,
        bool emitCommentSegments = false)
    {
        using var reader = new AjisStreamReader(stream, bufferSize);
        var parser = new AjisLexerParser(reader, numberOptions, stringOptions, commentOptions, textMode, allowTrailingCommas, allowDirectives, preserveStringEscapes, emitDirectiveSegments, emitCommentSegments);
        return parser.Parse();
    }

    /// <summary>
    /// Asynchronously parses AJIS text from a stream and yields segments one at a time.
    /// </summary>
    /// <remarks>
    /// This method uses <see cref="AjisLexerParserStreamingAsync"/> to emit segments as they are parsed,
    /// enabling memory-bounded processing of arbitrarily large documents. The stream is not closed by this method.
    /// </remarks>
    /// <param name="stream">Input stream containing AJIS text.</param>
    /// <param name="bufferSize">Buffer size for stream reading (default: 4096 bytes).</param>
    /// <param name="numberOptions">Numeric parsing options.</param>
    /// <param name="stringOptions">String parsing options.</param>
    /// <param name="commentOptions">Comment parsing options.</param>
    /// <param name="textMode">AJIS text mode (JSON/AJIS/Lex).</param>
    /// <param name="allowTrailingCommas">Allow trailing commas in objects and arrays.</param>
    /// <param name="allowDirectives">Allow AJIS directives.</param>
    /// <param name="preserveStringEscapes">Preserve escape sequences in string slices.</param>
    /// <param name="emitDirectiveSegments">Emit directive segments when encountered.</param>
    /// <param name="emitCommentSegments">Emit comment segments when encountered.</param>
    /// <param name="maxDepth">Maximum allowed nesting depth (default: 256).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable of <see cref="AjisSegment"/> objects.</returns>
    public static async IAsyncEnumerable<AjisSegment> ParseAsync(
        Stream stream,
        int bufferSize = 4096,
        global::Afrowave.AJIS.Core.AjisNumberOptions? numberOptions = null,
        global::Afrowave.AJIS.Core.AjisStringOptions? stringOptions = null,
        global::Afrowave.AJIS.Core.AjisCommentOptions? commentOptions = null,
        global::Afrowave.AJIS.Core.AjisTextMode textMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
        bool allowTrailingCommas = false,
        bool allowDirectives = true,
        bool preserveStringEscapes = false,
        bool emitDirectiveSegments = false,
        bool emitCommentSegments = false,
        int maxDepth = 256,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new AjisStreamReader(stream, bufferSize);
        var parser = new AjisLexerParserStreamingAsync(
            reader,
            numberOptions,
            stringOptions,
            commentOptions,
            textMode,
            allowTrailingCommas,
            allowDirectives,
            preserveStringEscapes,
            emitDirectiveSegments,
            emitCommentSegments,
            maxDepth);

        await foreach (var segment in parser.ParseAsync(ct).ConfigureAwait(false))
        {
            yield return segment;
        }
    }
}
