#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using System.Text;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Serializes segment streams into AJIS text.
/// </summary>
internal sealed class AjisSegmentTextWriter(AjisSerializationFormattingOptions options)
{
   private readonly StringBuilder _builder = new();
   private readonly Stack<ContainerContext> _stack = new();
   private readonly bool _compact = options.Compact;
   private readonly bool _pretty = options.Pretty;
   private readonly int _indentSize = options.IndentSize;

   public string Write(IEnumerable<AjisSegment> segments)
   {
      ArgumentNullException.ThrowIfNull(segments);

      foreach(AjisSegment segment in segments)
         AppendSegment(segment);

      return _builder.ToString();
   }

   public async Task WriteAsync(Stream output, IAsyncEnumerable<AjisSegment> segments, CancellationToken ct)
   {
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(segments);

      await foreach(AjisSegment segment in segments.WithCancellation(ct).ConfigureAwait(false))
         AppendSegment(segment);

      string text = _builder.ToString();
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      await output.WriteAsync(bytes, ct).ConfigureAwait(false);
   }

   private void AppendSegment(AjisSegment segment)
   {
      switch(segment.Kind)
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

   private void AppendContainerStart(AjisSegment segment)
   {
      if(segment.ContainerKind is null)
         throw new InvalidOperationException("Container segment missing ContainerKind.");

      WriteValueSeparatorIfNeeded();
      char token = segment.ContainerKind == AjisContainerKind.Object ? '{' : '[';
      _builder.Append(token);

      _stack.Push(new ContainerContext(segment.ContainerKind.Value));
   }

   private void AppendContainerEnd(AjisSegment segment)
   {
      if(segment.ContainerKind is null)
         throw new InvalidOperationException("Container segment missing ContainerKind.");

      if(_stack.Count == 0 || _stack.Peek().Kind != segment.ContainerKind.Value)
         throw new InvalidOperationException("Container end does not match current stack.");

      char token = segment.ContainerKind == AjisContainerKind.Object ? '}' : ']';
      ContainerContext context = _stack.Peek();
      if(_pretty && context.HasValue)
         AppendNewLineAndIndent(_stack.Count - 1);

      _builder.Append(token);
      _stack.Pop();

      if(_stack.TryPeek(out ContainerContext parent))
      {
         parent.HasValue = true;
         _stack.Pop();
         _stack.Push(parent);
      }
   }

   private void AppendPropertyName(AjisSegment segment)
   {
      if(segment.Slice is null)
         throw new InvalidOperationException("Property name segment missing slice.");

      EnsureInObject();
      ContainerContext context = _stack.Pop();

      if(context.HasValue)
      {
         _builder.Append(',');
         AppendValueSeparatorSpace();
      }
      else if(_pretty)
      {
         AppendNewLineAndIndent(_stack.Count);
      }

      if(_pretty && context.HasValue)
         AppendNewLineAndIndent(_stack.Count);

      WriteQuotedUtf8(segment.Slice.Value.Bytes.Span);
      _builder.Append(':');
      AppendNameSeparatorSpace();

      context.HasValue = true;
      _stack.Push(context);
   }

   private void AppendValue(AjisSegment segment)
   {
      WriteValueSeparatorIfNeeded();

      switch(segment.ValueKind)
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
            throw new InvalidOperationException("Value segment missing ValueKind.");
      }

      if(_stack.TryPeek(out ContainerContext parent))
      {
         parent.HasValue = true;
         _stack.Pop();
         _stack.Push(parent);
      }
   }

   private void WriteValueSeparatorIfNeeded()
   {
      if(!_stack.TryPeek(out ContainerContext context))
         return;

      if(context.Kind == AjisContainerKind.Array && context.HasValue)
      {
         _builder.Append(',');
         AppendValueSeparatorSpace();
      }
      else if(context.Kind == AjisContainerKind.Array && _pretty)
      {
         AppendNewLineAndIndent(_stack.Count);
      }

      if(context.Kind == AjisContainerKind.Array && context.HasValue && _pretty)
         AppendNewLineAndIndent(_stack.Count);
   }

   private void WriteBoolean(AjisSliceUtf8? slice)
   {
      if(slice is null)
         throw new InvalidOperationException("Boolean value segment missing slice.");

      _builder.Append(Encoding.UTF8.GetString(slice.Value.Bytes.Span));
   }

   private void WriteNumber(AjisSliceUtf8? slice)
   {
      if(slice is null)
         throw new InvalidOperationException("Number value segment missing slice.");

      _builder.Append(Encoding.UTF8.GetString(slice.Value.Bytes.Span));
   }

   private void WriteString(AjisSliceUtf8? slice)
   {
      if(slice is null)
         throw new InvalidOperationException("String value segment missing slice.");

      WriteQuotedUtf8(slice.Value.Bytes.Span);
   }

   private void WriteQuotedUtf8(ReadOnlySpan<byte> utf8)
   {
      _builder.Append('"');
      _builder.Append(AjisTextEscaper.EscapeUtf8(utf8));
      _builder.Append('"');
   }

   private void EnsureInObject()
   {
      if(_stack.Count == 0 || _stack.Peek().Kind != AjisContainerKind.Object)
         throw new InvalidOperationException("Property name encountered outside object context.");
   }

   private void AppendValueSeparatorSpace()
   {
      if(_pretty)
         return;

      if(!_compact)
         _builder.Append(' ');
   }

   private void AppendNameSeparatorSpace()
   {
      if(_pretty || !_compact)
         _builder.Append(' ');
   }

   private void AppendNewLineAndIndent(int depth)
   {
      _builder.AppendLine();
      _builder.Append(' ', depth * _indentSize);
   }

   private struct ContainerContext(AjisContainerKind kind)
   {
      public AjisContainerKind Kind { get; } = kind;
      public bool HasValue { get; set; }
   }
}
