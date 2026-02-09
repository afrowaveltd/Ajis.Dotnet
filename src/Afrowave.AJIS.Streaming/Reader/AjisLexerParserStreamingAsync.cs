#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using System.Runtime.CompilerServices;
using System.Text;

namespace Afrowave.AJIS.Streaming.Reader;

/// <summary>
/// Provides asynchronous streaming segment parsing with memory-bounded behavior.
/// </summary>
/// <remarks>
/// This parser emits <see cref="AjisSegment"/> objects one at a time through an <see cref="IAsyncEnumerable{T}"/>
/// rather than materializing them all in memory. This enables processing of arbitrarily large documents
/// with guaranteed bounded memory usage.
/// </remarks>
public sealed class AjisLexerParserStreamingAsync
{
    private readonly AjisLexer _lexer;
    private readonly global::Afrowave.AJIS.Core.AjisStringOptions _stringOptions;
    private readonly bool _allowTrailingCommas;
    private readonly bool _emitDirectiveSegments;
    private readonly bool _emitCommentSegments;
    private readonly int _maxDepth;
    private AjisToken _current;
    private int _depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="AjisLexerParserStreamingAsync"/> class.
    /// </summary>
    /// <param name="reader">The UTF-8 byte reader.</param>
    /// <param name="numberOptions">Numeric parsing options.</param>
    /// <param name="stringOptions">String parsing options.</param>
    /// <param name="commentOptions">Comment parsing options.</param>
    /// <param name="textMode">AJIS text mode (JSON/AJIS/Lex).</param>
    /// <param name="allowTrailingCommas">Allow trailing commas in objects and arrays.</param>
    /// <param name="allowDirectives">Allow AJIS directives.</param>
    /// <param name="preserveStringEscapes">Preserve escape sequences in string slices.</param>
    /// <param name="emitDirectiveSegments">Emit directive segments when encountered.</param>
    /// <param name="emitCommentSegments">Emit comment segments when encountered.</param>
    /// <param name="maxDepth">Maximum nesting depth (default: 128).</param>
    public AjisLexerParserStreamingAsync(
        IAjisReader reader,
        global::Afrowave.AJIS.Core.AjisNumberOptions? numberOptions = null,
        global::Afrowave.AJIS.Core.AjisStringOptions? stringOptions = null,
        global::Afrowave.AJIS.Core.AjisCommentOptions? commentOptions = null,
        global::Afrowave.AJIS.Core.AjisTextMode textMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
        bool allowTrailingCommas = false,
        bool allowDirectives = true,
        bool preserveStringEscapes = false,
        bool emitDirectiveSegments = false,
        bool emitCommentSegments = false,
        int maxDepth = 256)
    {
        if (maxDepth < 1)
            throw new ArgumentException("Max depth must be at least 1.", nameof(maxDepth));

        _lexer = new AjisLexer(reader, numberOptions, stringOptions, commentOptions, textMode, allowDirectives, preserveStringEscapes, emitCommentSegments);
        _stringOptions = stringOptions ?? new global::Afrowave.AJIS.Core.AjisStringOptions();
        _current = _lexer.NextToken();
        _allowTrailingCommas = allowTrailingCommas || textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex;
        _emitDirectiveSegments = emitDirectiveSegments;
        _emitCommentSegments = emitCommentSegments;
        _maxDepth = maxDepth;
        _depth = 0;
    }

    /// <summary>
    /// Asynchronously parses AJIS and yields segments one at a time.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable of <see cref="AjisSegment"/> objects.</returns>
    /// <exception cref="FormatException">Thrown when AJIS text is malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when nesting depth exceeds maximum.</exception>
    public async IAsyncEnumerable<AjisSegment> ParseAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var metaSegments1 = new List<AjisSegment>();
        EmitMetaTokens(metaSegments1);
        foreach (var segment in metaSegments1)
        {
            yield return segment;
        }

        await foreach (var segment in ParseValueAsync(ct))
        {
            yield return segment;
        }

        var metaSegments2 = new List<AjisSegment>();
        EmitMetaTokens(metaSegments2);
        foreach (var segment in metaSegments2)
        {
            yield return segment;
        }

        Expect(AjisTokenKind.End);
    }

    /// <summary>
    /// Emits meta tokens (directives and comments) synchronously.
    /// </summary>
    private void EmitMetaTokens(List<AjisSegment> segments)
    {
        while (_current.Kind is AjisTokenKind.Directive or AjisTokenKind.Comment)
        {
            if (_current.Kind == AjisTokenKind.Directive && _emitDirectiveSegments)
                segments.Add(AjisSegment.Directive(_current.Offset, _depth, CreateSlice(_current.Text, GetStringFlags(_current.Text))));

            if (_current.Kind == AjisTokenKind.Comment && _emitCommentSegments)
                segments.Add(AjisSegment.Comment(_current.Offset, _depth, CreateSlice(_current.Text, GetStringFlags(_current.Text))));

            Advance();
        }
    }

    /// <summary>
    /// Asynchronously parses a value (primitive or container).
    /// </summary>
    private async IAsyncEnumerable<AjisSegment> ParseValueAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var metaSegments = new List<AjisSegment>();
        EmitMetaTokens(metaSegments);
        foreach (var segment in metaSegments)
        {
            yield return segment;
        }

        switch (_current.Kind)
        {
            case AjisTokenKind.LeftBrace:
                await foreach (var segment in ParseObjectAsync(ct))
                {
                    yield return segment;
                }
                break;
            case AjisTokenKind.LeftBracket:
                await foreach (var segment in ParseArrayAsync(ct))
                {
                    yield return segment;
                }
                break;
            case AjisTokenKind.String:
                yield return AjisSegment.Value(_current.Offset, _depth, AjisValueKind.String, CreateSlice(_current.Text, GetStringFlags(_current.Text)));
                Advance();
                break;
            case AjisTokenKind.Number:
                yield return AjisSegment.Value(_current.Offset, _depth, AjisValueKind.Number, CreateSlice(_current.Text, GetNumberFlags(_current.Text)));
                Advance();
                break;
            case AjisTokenKind.True:
                yield return AjisSegment.Value(_current.Offset, _depth, AjisValueKind.Boolean, CreateSlice("true", AjisSliceFlags.None));
                Advance();
                break;
            case AjisTokenKind.False:
                yield return AjisSegment.Value(_current.Offset, _depth, AjisValueKind.Boolean, CreateSlice("false", AjisSliceFlags.None));
                Advance();
                break;
            case AjisTokenKind.Null:
                yield return AjisSegment.Value(_current.Offset, _depth, AjisValueKind.Null, null);
                Advance();
                break;
            default:
                throw new FormatException($"Unexpected token '{_current.Kind}' at {_current.Line}:{_current.Column}.");
        }
    }

    /// <summary>
    /// Asynchronously parses an object and yields its segments.
    /// </summary>
    private async IAsyncEnumerable<AjisSegment> ParseObjectAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ValidateDepth();

        var start = _current;
        yield return AjisSegment.Enter(AjisContainerKind.Object, start.Offset, _depth);
        _depth++;
        Advance();

        if (_current.Kind == AjisTokenKind.RightBrace)
        {
            var end = _current;
            _depth--;
            yield return AjisSegment.Exit(AjisContainerKind.Object, end.Offset, _depth);
            Advance();
            yield break;
        }

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var metaSegments = new List<AjisSegment>();
            EmitMetaTokens(metaSegments);
            foreach (var segment in metaSegments)
            {
                yield return segment;
            }

            if (_current.Kind != AjisTokenKind.String && _current.Kind != AjisTokenKind.Identifier)
                throw new FormatException($"Expected property name at {_current.Line}:{_current.Column}.");

            EnsurePropertyNameLimit(_current.Text, _current.Offset);
            var nameFlags = _current.Kind == AjisTokenKind.Identifier
                ? AjisSliceFlags.IsIdentifierStyle | GetStringFlags(_current.Text)
                : GetStringFlags(_current.Text);
            yield return AjisSegment.Name(_current.Offset, _depth, CreateSlice(_current.Text, nameFlags));
            Advance();

            var metaSegments2 = new List<AjisSegment>();
            EmitMetaTokens(metaSegments2);
            foreach (var segment in metaSegments2)
            {
                yield return segment;
            }

            Expect(AjisTokenKind.Colon);

            await foreach (var segment in ParseValueAsync(ct))
            {
                yield return segment;
            }

            var metaSegments3 = new List<AjisSegment>();
            EmitMetaTokens(metaSegments3);
            foreach (var segment in metaSegments3)
            {
                yield return segment;
            }

            if (_current.Kind == AjisTokenKind.Comma)
            {
                Advance();
                var metaSegments4 = new List<AjisSegment>();
                EmitMetaTokens(metaSegments4);
                foreach (var segment in metaSegments4)
                {
                    yield return segment;
                }
                if (_current.Kind == AjisTokenKind.RightBrace)
                {
                    if (!_allowTrailingCommas)
                        throw new FormatException($"Trailing commas are not allowed at {_current.Line}:{_current.Column}.");
                }
                else
                {
                    continue;
                }
            }

            if (_current.Kind == AjisTokenKind.RightBrace)
            {
                var end = _current;
                _depth--;
                yield return AjisSegment.Exit(AjisContainerKind.Object, end.Offset, _depth);
                Advance();
                yield break;
            }

            throw new FormatException($"Expected ',' or '}}' at {_current.Line}:{_current.Column}.");
        }
    }

    /// <summary>
    /// Asynchronously parses an array and yields its segments.
    /// </summary>
    private async IAsyncEnumerable<AjisSegment> ParseArrayAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ValidateDepth();

        var start = _current;
        yield return AjisSegment.Enter(AjisContainerKind.Array, start.Offset, _depth);
        _depth++;
        Advance();

        if (_current.Kind == AjisTokenKind.RightBracket)
        {
            var end = _current;
            _depth--;
            yield return AjisSegment.Exit(AjisContainerKind.Array, end.Offset, _depth);
            Advance();
            yield break;
        }

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var metaSegments = new List<AjisSegment>();
            EmitMetaTokens(metaSegments);
            foreach (var segment in metaSegments)
            {
                yield return segment;
            }

            await foreach (var segment in ParseValueAsync(ct))
            {
                yield return segment;
            }

            var metaSegments2 = new List<AjisSegment>();
            EmitMetaTokens(metaSegments2);
            foreach (var segment in metaSegments2)
            {
                yield return segment;
            }

            if (_current.Kind == AjisTokenKind.Comma)
            {
                Advance();
                var metaSegments3 = new List<AjisSegment>();
                EmitMetaTokens(metaSegments3);
                foreach (var segment in metaSegments3)
                {
                    yield return segment;
                }
                if (_current.Kind == AjisTokenKind.RightBracket)
                {
                    if (!_allowTrailingCommas)
                        throw new FormatException($"Trailing commas are not allowed at {_current.Line}:{_current.Column}.");
                }
                else
                {
                    continue;
                }
            }

            if (_current.Kind == AjisTokenKind.RightBracket)
            {
                var end = _current;
                _depth--;
                yield return AjisSegment.Exit(AjisContainerKind.Array, end.Offset, _depth);
                Advance();
                yield break;
            }

            throw new FormatException($"Expected ',' or ']' at {_current.Line}:{_current.Column}.");
        }
    }

    /// <summary>
    /// Validates that the current nesting depth does not exceed the maximum allowed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when max depth is exceeded.</exception>
    private void ValidateDepth()
    {
        if (_depth >= _maxDepth)
            throw new InvalidOperationException($"Maximum nesting depth ({_maxDepth}) exceeded at offset {_current.Offset}.");
    }

    /// <summary>
    /// Ensures property name does not exceed size limits.
    /// </summary>
    private void EnsurePropertyNameLimit(string? text, long offset)
    {
        if (_stringOptions.MaxPropertyNameBytes is not int max || max <= 0)
            return;

        int byteCount = text is null ? 0 : Encoding.UTF8.GetByteCount(text);
        if (byteCount > max)
            throw new FormatException($"Property name exceeds maximum size at offset {offset}.");
    }

    /// <summary>
    /// Gets slice flags for numeric values (hex, binary, octal, typed literal).
    /// </summary>
    private static AjisSliceFlags GetNumberFlags(string? text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 2)
            return AjisSliceFlags.None;

        if (IsTypedLiteral(text))
            return AjisSliceFlags.IsNumberTyped;

        return text[0] == '0' && (text[1] == 'x' || text[1] == 'X')
            ? AjisSliceFlags.IsNumberHex
            : text[0] == '0' && (text[1] == 'b' || text[1] == 'B')
                ? AjisSliceFlags.IsNumberBinary
                : text[0] == '0' && (text[1] == 'o' || text[1] == 'O')
                    ? AjisSliceFlags.IsNumberOctal
                    : AjisSliceFlags.None;
    }

    /// <summary>
    /// Determines if text represents a typed literal (e.g., T1234567890).
    /// </summary>
    private static bool IsTypedLiteral(string text)
    {
        int i = 0;
        while (i < text.Length && text[i] is >= 'A' and <= 'Z')
            i++;

        int prefixEnd = i;
        if (prefixEnd == 0 || prefixEnd == text.Length)
            return false;

        while (i < text.Length && text[i] is >= '0' and <= '9')
            i++;

        if (i == prefixEnd)
            return false;

        return i == text.Length;
    }

    /// <summary>
    /// Gets slice flags for string values (escapes, non-ASCII, identifier style).
    /// </summary>
    private static AjisSliceFlags GetStringFlags(string? text)
    {
        if (string.IsNullOrEmpty(text)) return AjisSliceFlags.None;

        AjisSliceFlags flags = AjisSliceFlags.None;
        foreach (char c in text)
        {
            if (c == '\\')
                flags |= AjisSliceFlags.HasEscapes;
            if (c > 0x7F)
                flags |= AjisSliceFlags.HasNonAscii;
        }

        return flags;
    }

    /// <summary>
    /// Creates an UTF-8 slice from text with specified flags.
    /// </summary>
    private static AjisSliceUtf8 CreateSlice(string? text, AjisSliceFlags flags)
        => new(text is null ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(text), flags);

    /// <summary>
    /// Expects the current token to be of a specific kind, advancing if matched.
    /// </summary>
    /// <exception cref="FormatException">Thrown if expected token kind is not matched.</exception>
    private void Expect(AjisTokenKind kind)
    {
        if (_current.Kind != kind)
            throw new FormatException($"Expected '{kind}' at {_current.Line}:{_current.Column}.");

        Advance();
    }

    /// <summary>
    /// Advances to the next token.
    /// </summary>
    private void Advance() => _current = _lexer.NextToken();
}
