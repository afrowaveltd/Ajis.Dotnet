#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using System.Text;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Converts <see cref="AjisSegment"/> streams into formatted AJIS text output.
/// </summary>
/// <remarks>
/// <para>
/// This writer consumes segments and produces valid AJIS text with configurable formatting.
/// It uses a stack-based approach to track container nesting and emit appropriate separators.
/// </para>
/// <para>
/// Supported formatting modes:
/// - Compact: minimal whitespace for size optimization
/// - Pretty: human-readable with indentation and newlines
/// - Canonical: deterministic output for hashing/diffing
/// </para>
/// </remarks>
internal sealed class AjisSegmentTextWriter(AjisSerializationFormattingOptions options)
{
    private readonly StringBuilder _builder = new();
    private readonly Stack<ContainerContext> _stack = new();
    private readonly bool _compact = options.Compact;
    private readonly bool _pretty = options.Pretty;
    private readonly int _indentSize = options.IndentSize;

    /// <summary>
    /// Synchronously writes all segments to a string.
    /// </summary>
    /// <param name="segments">The segments to serialize.</param>
    /// <returns>The formatted AJIS text.</returns>
    /// <exception cref="ArgumentNullException">Thrown if segments is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if segments are malformed (e.g., mismatched containers).</exception>
    public string Write(IEnumerable<AjisSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        foreach (AjisSegment segment in segments)
            AppendSegment(segment);

        return _builder.ToString();
    }

    /// <summary>
    /// Asynchronously writes segments from an async enumerable to an output stream.
    /// </summary>
    /// <param name="output">The output stream to write UTF-8 encoded text to.</param>
    /// <param name="segments">The async enumerable of segments to serialize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if output or segments is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if segments are malformed.</exception>
    public async Task WriteAsync(Stream output, IAsyncEnumerable<AjisSegment> segments, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(segments);

        await foreach (AjisSegment segment in segments.WithCancellation(ct).ConfigureAwait(false))
            AppendSegment(segment);

        string text = _builder.ToString();
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await output.WriteAsync(bytes, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes a single segment and appends appropriate text to the builder.
    /// </summary>
    /// <remarks>
    /// Comments and directives are skipped as they are meta-segments and should not appear in output.
    /// </remarks>
    private void AppendSegment(AjisSegment segment)
    {
        switch (segment.Kind)
        {
            case AjisSegmentKind.EnterContainer:
                AppendContainerStart(segment);
                break;
            case AjisSegmentKind.ExitContainer:
                AppendContainerEnd(segment);
                break;
            case AjisSegmentKind.PropertyName:
                AppendPropertyName(segment);
                break;
            case AjisSegmentKind.Value:
                AppendValue(segment);
                break;
            case AjisSegmentKind.Comment:
            case AjisSegmentKind.Directive:
                return;
            default:
                throw new InvalidOperationException($"Unsupported segment kind: {segment.Kind}.");
        }
    }

    /// <summary>
    /// Appends opening bracket for a container ({or [).
    /// </summary>
    /// <remarks>
    /// Pushes a new container context onto the stack to track nesting and value presence.
    /// </remarks>
    private void AppendContainerStart(AjisSegment segment)
    {
        if (segment.ContainerKind is null)
            throw new InvalidOperationException("Container segment missing ContainerKind.");

        WriteValueSeparatorIfNeeded();
        char token = segment.ContainerKind == AjisContainerKind.Object ? '{' : '[';
        _builder.Append(token);

        _stack.Push(new ContainerContext(segment.ContainerKind.Value));
    }

    /// <summary>
    /// Appends closing bracket for a container (} or ]).
    /// </summary>
    /// <remarks>
    /// Validates that the closing container matches the innermost open container.
    /// Adds indentation for pretty mode.
    /// </remarks>
    private void AppendContainerEnd(AjisSegment segment)
    {
        if (segment.ContainerKind is null)
            throw new InvalidOperationException("Container segment missing ContainerKind.");

        if (_stack.Count == 0 || _stack.Peek().Kind != segment.ContainerKind.Value)
            throw new InvalidOperationException("Container end does not match current stack.");

        char token = segment.ContainerKind == AjisContainerKind.Object ? '}' : ']';
        ContainerContext context = _stack.Peek();
        if (_pretty && context.HasValue)
            AppendNewLineAndIndent(_stack.Count - 1);

        _builder.Append(token);
        _stack.Pop();

        if (_stack.TryPeek(out ContainerContext parent))
        {
            parent.HasValue = true;
            _stack.Pop();
            _stack.Push(parent);
        }
    }

    /// <summary>
    /// Appends a property name (object key) with surrounding quotes and colon.
    /// </summary>
    /// <remarks>
    /// Property names are always quoted, even if they are identifiers.
    /// Preceded by comma if not the first property (except in pretty mode where newline is used).
    /// </remarks>
    private void AppendPropertyName(AjisSegment segment)
    {
        if (segment.Slice is null)
            throw new InvalidOperationException("Property name segment missing slice.");

        EnsureInObject();
        ContainerContext context = _stack.Pop();

        if (context.HasValue)
        {
            _builder.Append(',');
            AppendValueSeparatorSpace();
        }
        else if (_pretty)
        {
            AppendNewLineAndIndent(_stack.Count);
        }

        if (_pretty && context.HasValue)
            AppendNewLineAndIndent(_stack.Count);

        WriteQuotedUtf8(segment.Slice.Value.Bytes.Span);
        _builder.Append(':');
        AppendNameSeparatorSpace();

        context.HasValue = true;
        _stack.Push(context);
    }

    /// <summary>
    /// Appends a value (null, boolean, number, or string).
    /// </summary>
    /// <remarks>
    /// For arrays, adds comma between values. For all containers in pretty mode, adds newlines and indentation.
    /// </remarks>
    private void AppendValue(AjisSegment segment)
    {
        WriteValueSeparatorIfNeeded();

        switch (segment.ValueKind)
        {
            case AjisValueKind.Null:
                _builder.Append("null");
                break;
            case AjisValueKind.Boolean:
                WriteBoolean(segment.Slice);
                break;
            case AjisValueKind.Number:
                WriteNumber(segment.Slice);
                break;
            case AjisValueKind.String:
                WriteString(segment.Slice);
                break;
            default:
                throw new InvalidOperationException($"Unsupported segment kind: {segment.Kind}.");
        }

        if (_stack.TryPeek(out ContainerContext parent))
        {
            parent.HasValue = true;
            _stack.Pop();
            _stack.Push(parent);
        }
    }

    /// <summary>
    /// Emits value separators (commas, newlines) for array elements.
    /// </summary>
    /// <remarks>
    /// For arrays: adds comma between items in compact mode, newlines in pretty mode.
    /// Does nothing for objects (object properties use comma-separated format).
    /// </remarks>
    private void WriteValueSeparatorIfNeeded()
    {
        if (!_stack.TryPeek(out ContainerContext context))
            return;

        if (context.Kind == AjisContainerKind.Array && context.HasValue)
        {
            _builder.Append(',');
            AppendValueSeparatorSpace();
        }
        else if (context.Kind == AjisContainerKind.Array && _pretty)
        {
            AppendNewLineAndIndent(_stack.Count);
        }

        if (context.Kind == AjisContainerKind.Array && context.HasValue && _pretty)
            AppendNewLineAndIndent(_stack.Count);
    }

    /// <summary>
    /// Writes a boolean value (true or false) without quotes.
    /// </summary>
    private void WriteBoolean(AjisSliceUtf8? slice)
    {
        if (slice is null)
            throw new InvalidOperationException("Boolean value segment missing slice.");

        _builder.Append(Encoding.UTF8.GetString(slice.Value.Bytes.Span));
    }

    /// <summary>
    /// Writes a numeric value preserving its original format/base if indicated by slice flags.
    /// </summary>
    /// <remarks>
    /// Numbers are written as-is without modification, preserving hex/binary/octal prefixes.
    /// </remarks>
    private void WriteNumber(AjisSliceUtf8? slice)
    {
        if (slice is null)
            throw new InvalidOperationException("Number value segment missing slice.");

        _builder.Append(Encoding.UTF8.GetString(slice.Value.Bytes.Span));
    }

    /// <summary>
    /// Writes a string value with surrounding quotes and proper escape sequences.
    /// </summary>
    private void WriteString(AjisSliceUtf8? slice)
    {
        if (slice is null)
            throw new InvalidOperationException("String value segment missing slice.");

        WriteQuotedUtf8(slice.Value.Bytes.Span);
    }

    /// <summary>
    /// Writes a UTF-8 byte sequence with surrounding quotes and escape sequences applied.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="AjisTextEscaper"/> to handle quote, backslash, control character escaping.
    /// </remarks>
    private void WriteQuotedUtf8(ReadOnlySpan<byte> utf8)
    {
        _builder.Append('"');
        _builder.Append(AjisTextEscaper.EscapeUtf8(utf8));
        _builder.Append('"');
    }

    /// <summary>
    /// Ensures that the current context is an object.
    /// </summary>
    /// <remarks>
    /// Called before appending a property name. Throws if not inside an object.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if not in object context.</exception>
    private void EnsureInObject()
    {
        if (_stack.Count == 0 || _stack.Peek().Kind != AjisContainerKind.Object)
            throw new InvalidOperationException("Property name encountered outside object context.");
    }

    /// <summary>
    /// Appends spacing after value separators (commas, colons) based on formatting mode.
    /// </summary>
    /// <remarks>
    /// Compact mode: no space
    /// Non-compact mode: one space (unless pretty mode handles newline)
    /// Pretty mode: space is returned, newline handled elsewhere
    /// </remarks>
    private void AppendValueSeparatorSpace()
    {
        if (_pretty)
            return;

        if (!_compact)
            _builder.Append(' ');
    }

    /// <summary>
    /// Appends spacing after property name colons based on formatting mode.
    /// </summary>
    /// <remarks>
    /// Always adds space unless in compact mode.
    /// </remarks>
    private void AppendNameSeparatorSpace()
    {
        if (_pretty || !_compact)
            _builder.Append(' ');
    }

    /// <summary>
    /// Appends a newline followed by indentation for the given nesting depth.
    /// </summary>
    /// <remarks>
    /// Used in pretty mode to format nested structures.
    /// Depth is multiplied by <see cref="_indentSize"/> to get character count.
    /// </remarks>
    private void AppendNewLineAndIndent(int depth)
    {
        _builder.AppendLine();
        _builder.Append(' ', depth * _indentSize);
    }

    /// <summary>
    /// Tracks context for a single container (object or array).
    /// </summary>
    /// <remarks>
    /// <see cref="HasValue"/> indicates whether any members/elements have been written.
    /// Used to determine when to emit separators and formatting.
    /// </remarks>
    private struct ContainerContext(AjisContainerKind kind)
    {
        /// <summary>Gets the container kind (Object or Array).</summary>
        public AjisContainerKind Kind { get; } = kind;

        /// <summary>Gets or sets whether this container has had any values/members written.</summary>
        public bool HasValue { get; set; }
    }
}
