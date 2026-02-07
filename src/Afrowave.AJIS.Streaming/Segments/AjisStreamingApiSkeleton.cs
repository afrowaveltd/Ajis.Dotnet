#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming.Reader;
using Afrowave.AJIS.Streaming.Segments.Engines;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Afrowave.AJIS.Streaming.Segments;

/// <summary>
/// Indicates what kind of container is currently being entered/exited.
/// </summary>
public enum AjisContainerKind
{
   /// <summary>
   /// Object container (<c>{ ... }</c>).
   /// </summary>
   Object = 0,

   /// <summary>
   /// Array container (<c>[ ... ]</c>).
   /// </summary>
   Array = 1,
}

/// <summary>
/// Indicates a primitive value kind.
/// </summary>
public enum AjisValueKind
{
   Null = 0,
   Boolean = 1,
   Number = 2,
   String = 3,
}

/// <summary>
/// Segment kinds emitted by the streaming parser.
/// </summary>
public enum AjisSegmentKind
{
   /// <summary>
   /// Entering a container (<c>{</c> or <c>[</c>).
   /// </summary>
   EnterContainer = 0,

   /// <summary>
   /// Exiting a container (<c>}</c> or <c>]</c>).
   /// </summary>
   ExitContainer = 1,

   /// <summary>
   /// A property name inside an object.
   /// </summary>
   PropertyName = 2,

   /// <summary>
   /// A primitive value.
   /// </summary>
   Value = 3,

   /// <summary>
   /// A comment token emitted when enabled by options.
   /// </summary>
   Comment = 4,

   /// <summary>
   /// A directive token emitted when enabled by options.
   /// </summary>
   Directive = 5,
}

/// <summary>
/// A single streaming segment.
/// </summary>
public sealed record AjisSegment(
   AjisSegmentKind Kind,
   long Position,
   int Depth,
   AjisContainerKind? ContainerKind,
   AjisValueKind? ValueKind,
   AjisSliceUtf8? Slice)
{
   public static AjisSegment Enter(AjisContainerKind kind, long pos, int depth)
       => new(AjisSegmentKind.EnterContainer, pos, depth, kind, null, null);

   public static AjisSegment Exit(AjisContainerKind kind, long pos, int depth)
       => new(AjisSegmentKind.ExitContainer, pos, depth, kind, null, null);

   /// <summary>
   /// Creates a property-name segment with an associated slice.
   /// </summary>
   public static AjisSegment Name(long pos, int depth, AjisSliceUtf8 slice)
       => new(AjisSegmentKind.PropertyName, pos, depth, null, null, slice);

   /// <summary>
   /// Creates a value segment with an optional slice.
   /// </summary>
   public static AjisSegment Value(long pos, int depth, AjisValueKind kind, AjisSliceUtf8? slice = null)
       => new(AjisSegmentKind.Value, pos, depth, null, kind, slice);

   public static AjisSegment Comment(long pos, int depth, AjisSliceUtf8? slice)
       => new(AjisSegmentKind.Comment, pos, depth, null, null, slice);

   public static AjisSegment Directive(long pos, int depth, AjisSliceUtf8? slice)
       => new(AjisSegmentKind.Directive, pos, depth, null, null, slice);
}

/// <summary>
/// Streaming parser entry points.
/// </summary>
public static class AjisParse
{
   /// <summary>
   /// Represents a parsing result with applied settings.
   /// </summary>
   public readonly record struct AjisParseResult(IReadOnlyList<AjisSegment> Segments, AjisSettings Settings);

   /// <summary>
   /// Parses AJIS text from a <see cref="Stream"/> and emits segments.
   /// </summary>
   /// <remarks>
   /// <para>
   /// This method is designed for huge files (hundreds of MB to multi-GB) and can operate
   /// with low memory usage.
   /// </para>
   /// <para>
   /// It must stop at the end-of-text boundary to enable ATP (text+binary) usage.
   /// </para>
   /// </remarks>
   /// <param name="input">Input stream positioned at the start of AJIS text.</param>
   /// <param name="settings">AJIS settings.</param>
   /// <param name="ct">Cancellation token.</param>
   /// <returns>An async stream of segments.</returns>
   public static async IAsyncEnumerable<AjisSegment> ParseSegmentsAsync(
       Stream input,
       AjisSettings? settings = null,
       [EnumeratorCancellation] CancellationToken ct = default)
   {
      _ = input ?? throw new ArgumentNullException(nameof(input));
      AjisProcessingProfile profile = settings?.ParserProfile ?? AjisProcessingProfile.Universal;
      _ = AjisSegmentParseEngineSelector.Select(profile, AjisSegmentParseInputKind.Stream);
      _ = ct;

      var eventSink = settings?.EventSink ?? global::Afrowave.AJIS.Core.Events.NullAjisEventSink.Instance;
      long? totalBytes = input.CanSeek ? input.Length : null;
      await eventSink.EmitAsync(
         new global::Afrowave.AJIS.Core.Events.AjisMilestoneEvent(DateTimeOffset.UtcNow, "parse", "start"),
         ct).ConfigureAwait(false);

      await eventSink.EmitAsync(
         new global::Afrowave.AJIS.Core.Events.AjisProgressEvent(DateTimeOffset.UtcNow, "parse", 0, 0, totalBytes),
         ct).ConfigureAwait(false);

      if(settings is not null)
         _ = ResolveChunkThresholdBytes(settings);

      if(profile == AjisProcessingProfile.Universal)
      {
         foreach(var segment in AjisLexerParserStream.Parse(
            input,
            numberOptions: settings?.Numbers,
            stringOptions: settings?.Strings,
            commentOptions: settings?.Comments,
            textMode: settings?.TextMode ?? global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
            allowTrailingCommas: settings?.AllowTrailingCommas ?? false,
            allowDirectives: settings?.AllowDirectives ?? true,
            preserveStringEscapes: true,
            emitDirectiveSegments: settings?.AllowDirectives ?? true,
            emitCommentSegments: true))
            yield return segment;

         await EmitParseCompletionAsync(eventSink, totalBytes, ct).ConfigureAwait(false);
         yield break;
      }

      if(RequiresLexerParsing(settings))
      {
         foreach(var segment in AjisLexerParserStream.Parse(
            input,
            numberOptions: settings?.Numbers,
            stringOptions: settings?.Strings,
            commentOptions: settings?.Comments,
            textMode: settings?.TextMode ?? global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
            allowTrailingCommas: settings?.AllowTrailingCommas ?? false,
            allowDirectives: settings?.AllowDirectives ?? true,
            preserveStringEscapes: true,
            emitDirectiveSegments: settings?.AllowDirectives ?? true,
            emitCommentSegments: true))
            yield return segment;

         await EmitParseCompletionAsync(eventSink, totalBytes, ct).ConfigureAwait(false);
         yield break;
      }

      if(input is MemoryStream mem && mem.TryGetBuffer(out ArraySegment<byte> buffer))
      {
         _ = AjisSegmentParseEngineSelector.Select(profile, AjisSegmentParseInputKind.Span);
         foreach(var segment in ParseSegments(buffer.AsSpan(), settings))
            yield return segment;

         await EmitParseCompletionAsync(eventSink, totalBytes, ct).ConfigureAwait(false);
         yield break;
      }

      if(input is FileStream fileStream && fileStream.CanSeek)
      {
         _ = AjisSegmentParseEngineSelector.Select(profile, AjisSegmentParseInputKind.Stream);
         foreach(var segment in ParseSegmentsMappedFile(fileStream.Name, settings))
            yield return segment;

         await EmitParseCompletionAsync(eventSink, totalBytes, ct).ConfigureAwait(false);
         yield break;
      }

      string tempPath = Path.GetTempFileName();
      try
      {
         await using(var tempStream = File.Create(tempPath))
            await input.CopyToAsync(tempStream, ct).ConfigureAwait(false);

         _ = AjisSegmentParseEngineSelector.Select(profile, AjisSegmentParseInputKind.Stream);
         foreach(var segment in ParseSegmentsMappedFile(tempPath, settings))
            yield return segment;

         await EmitParseCompletionAsync(eventSink, totalBytes, ct).ConfigureAwait(false);
      }
      finally
      {
         if(File.Exists(tempPath))
            File.Delete(tempPath);
      }
   }

   /// <summary>
   /// Parses AJIS text from a <see cref="Stream"/>, applies document directives, and returns segments with updated settings.
   /// </summary>
   public static async ValueTask<AjisParseResult> ParseSegmentsWithDirectivesAsync(
      Stream input,
      AjisSettings settings,
      CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(input);
      ArgumentNullException.ThrowIfNull(settings);

      var segments = new List<AjisSegment>();
      await foreach(AjisSegment segment in ParseSegmentsAsync(input, settings, ct).ConfigureAwait(false))
         segments.Add(segment);

      AjisSettings applied = global::Afrowave.AJIS.Streaming.Segments.Transforms.AjisDirectiveSettingsApplier
         .ApplyDocumentDirectives(segments, settings);

      return new AjisParseResult(segments, applied);
   }

   private static bool RequiresLexerParsing(AjisSettings? settings)
   {
      if(settings is null)
         return false;

      if(settings.TextMode != global::Afrowave.AJIS.Core.AjisTextMode.Json)
         return true;

      if(settings.AllowDirectives || settings.AllowTrailingCommas)
         return true;

      if(settings.Comments.AllowBlockComments || settings.Comments.AllowLineComments)
         return true;

      if(settings.Strings.AllowUnquotedPropertyNames || settings.Strings.AllowMultiline || settings.Strings.AllowSingleQuotes)
         return true;

      if(settings.Numbers.EnableBasePrefixes || settings.Numbers.EnableDigitSeparators
         || settings.Numbers.AllowNaNAndInfinity || settings.Numbers.AllowLeadingPlusOnNumbers)
         return true;

      return false;
   }

   private static async ValueTask EmitParseCompletionAsync(
      global::Afrowave.AJIS.Core.Events.IAjisEventSink eventSink,
      long? totalBytes,
      CancellationToken ct)
   {
      long processed = totalBytes ?? 0;
      int percent = totalBytes is null ? 0 : 100;

      await eventSink.EmitAsync(
         new global::Afrowave.AJIS.Core.Events.AjisProgressEvent(DateTimeOffset.UtcNow, "parse", percent, processed, totalBytes),
         ct).ConfigureAwait(false);

      await eventSink.EmitAsync(
         new global::Afrowave.AJIS.Core.Events.AjisMilestoneEvent(DateTimeOffset.UtcNow, "parse", "end"),
         ct).ConfigureAwait(false);
   }

   /// <summary>
   /// Parses AJIS text from a <see cref="ReadOnlySpan{T}"/> and emits segments.
   /// </summary>
   /// <remarks>
   /// This overload targets in-memory buffers (e.g. network payload already buffered).
   /// </remarks>
   public static IEnumerable<AjisSegment> ParseSegments(
       ReadOnlySpan<byte> utf8,
       AjisSettings? settings = null)
   {
      AjisProcessingProfile profile = settings?.ParserProfile ?? AjisProcessingProfile.Universal;
      _ = AjisSegmentParseEngineSelector.Select(profile, AjisSegmentParseInputKind.Span);
      return ParseSegmentsInternal(utf8, settings);
   }

   /// <summary>
   /// Parses AJIS text from a <see cref="ReadOnlySpan{T}"/> and applies document directives.
   /// </summary>
   public static AjisParseResult ParseSegmentsWithDirectives(
      ReadOnlySpan<byte> utf8,
      AjisSettings settings)
   {
      ArgumentNullException.ThrowIfNull(settings);

      List<AjisSegment> segments = [.. ParseSegments(utf8, settings)];
      AjisSettings applied = global::Afrowave.AJIS.Streaming.Segments.Transforms.AjisDirectiveSettingsApplier
         .ApplyDocumentDirectives(segments, settings);

      return new AjisParseResult(segments, applied);
   }

   private static IEnumerable<AjisSegment> ParseSegmentsInternal(ReadOnlySpan<byte> utf8, AjisSettings? settings)
   {
      var reader = new AjisSpanReader(utf8.ToArray());
      var parser = new AjisLexerParser(
         reader,
         settings?.Numbers,
         settings?.Strings,
         settings?.Comments,
         settings?.TextMode ?? global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
         settings?.AllowTrailingCommas ?? false,
         settings?.AllowDirectives ?? true,
         preserveStringEscapes: true,
         emitDirectiveSegments: settings?.AllowDirectives ?? true,
         emitCommentSegments: true);
      return parser.Parse();
   }

   private static string GetNumberText(Utf8JsonReader reader)
   {
      if(!reader.HasValueSequence)
         return Encoding.UTF8.GetString(reader.ValueSpan);

      return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
   }

   private static AjisSliceFlags GetNumberFlags(string? text)
   {
      if(string.IsNullOrEmpty(text) || text.Length < 2)
         return AjisSliceFlags.None;

      return text[0] == '0' && (text[1] == 'x' || text[1] == 'X')
         ? AjisSliceFlags.IsNumberHex
         : text[0] == '0' && (text[1] == 'b' || text[1] == 'B')
            ? AjisSliceFlags.IsNumberBinary
            : text[0] == '0' && (text[1] == 'o' || text[1] == 'O')
               ? AjisSliceFlags.IsNumberOctal
               : AjisSliceFlags.None;
   }

   private static AjisSliceFlags GetStringFlags(string? text)
   {
      if(string.IsNullOrEmpty(text)) return AjisSliceFlags.None;

      AjisSliceFlags flags = AjisSliceFlags.None;
      foreach(char c in text)
      {
         if(c == '\\')
            flags |= AjisSliceFlags.HasEscapes;
         if(c > 0x7F)
            flags |= AjisSliceFlags.HasNonAscii;
      }

      return flags;
   }

   private static AjisSliceUtf8 CreateSlice(string? text, AjisSliceFlags flags)
      => new(text is null ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(text), flags);

   private static IEnumerable<AjisSegment> ParseSegmentsMappedFile(string path, AjisSettings? settings)
   {
      long length = new FileInfo(path).Length;
      using var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

      if(length == 0)
         return [];
      long thresholdBytes = ResolveChunkThresholdBytes(settings);

      if(length > thresholdBytes)
         return ParseSegmentsMappedFileChunked(path, settings, thresholdBytes);

      if(length > int.MaxValue)
         return ParseSegmentsMappedFileChunked(path, settings, thresholdBytes);

      using var accessor = mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);

      unsafe
      {
         byte* ptr = null;
         accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
         try
         {
            ptr += accessor.PointerOffset;
            var span = new ReadOnlySpan<byte>(ptr, (int)length);

            return [.. ParseSegments(span, settings)];
         }
         finally
         {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
         }
      }

   }

   private static IEnumerable<AjisSegment> ParseSegmentsMappedFileChunked(
      string path,
      AjisSettings? settings,
      long thresholdBytes)
   {
      if(thresholdBytes > int.MaxValue)
         throw new NotSupportedException("Chunk threshold exceeds supported segment size.");

      long length = new FileInfo(path).Length;
      if(length <= 0)
         return [];

      using var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
      int chunkSize = (int)Math.Min(int.MaxValue, Math.Max(1, thresholdBytes));

      byte[] buffer = ArrayPool<byte>.Shared.Rent(chunkSize * 2);
      var segments = new List<AjisSegment>();
      long offset = 0;
      int carry = 0;
      var state = new JsonReaderState(new JsonReaderOptions { CommentHandling = JsonCommentHandling.Disallow });

      try
      {
         while(offset < length)
         {
            int take = (int)Math.Min(chunkSize, length - offset);
            using var accessor = mmf.CreateViewAccessor(offset, take, MemoryMappedFileAccess.Read);
            accessor.ReadArray(0, buffer, carry, take);

            bool isFinal = offset + take >= length;
            var span = new ReadOnlySpan<byte>(buffer, 0, carry + take);
            var reader = new Utf8JsonReader(span, isFinal, state);

            while(reader.Read())
            {
               long tokenOffset = offset - carry + reader.TokenStartIndex;
               int depth = reader.CurrentDepth;

               switch(reader.TokenType)
               {
                  case JsonTokenType.StartObject:
                     segments.Add(AjisSegment.Enter(AjisContainerKind.Object, tokenOffset, depth));
                     break;
                  case JsonTokenType.EndObject:
                     segments.Add(AjisSegment.Exit(AjisContainerKind.Object, tokenOffset, depth));
                     break;
                  case JsonTokenType.StartArray:
                     segments.Add(AjisSegment.Enter(AjisContainerKind.Array, tokenOffset, depth));
                     break;
                  case JsonTokenType.EndArray:
                     segments.Add(AjisSegment.Exit(AjisContainerKind.Array, tokenOffset, depth));
                     break;
                  case JsonTokenType.PropertyName:
                     {
                        string nameText = reader.GetString() ?? string.Empty;
                        segments.Add(AjisSegment.Name(tokenOffset, depth, CreateSlice(nameText, GetStringFlags(nameText))));
                        break;
                     }
                  case JsonTokenType.String:
                     {
                        string stringText = reader.GetString() ?? string.Empty;
                        segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.String, CreateSlice(stringText, GetStringFlags(stringText))));
                        break;
                     }
                  case JsonTokenType.Number:
                     {
                        string numberText = GetNumberText(reader);
                        segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Number, CreateSlice(numberText, GetNumberFlags(numberText))));
                        break;
                     }
                  case JsonTokenType.True:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Boolean, CreateSlice("true", AjisSliceFlags.None)));
                     break;
                  case JsonTokenType.False:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Boolean, CreateSlice("false", AjisSliceFlags.None)));
                     break;
                  case JsonTokenType.Null:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Null));
                     break;
               }
            }

            state = reader.CurrentState;
            int consumed = (int)reader.BytesConsumed;
            carry = span.Length - consumed;
            if(carry > 0)
               span[consumed..].CopyTo(buffer);
            offset += take;
         }
      }
      finally
      {
         ArrayPool<byte>.Shared.Return(buffer);
      }

      return segments;
   }

   private static long ResolveChunkThresholdBytes(AjisSettings? settings)
   {
      string raw = settings?.StreamChunkThreshold ?? "2G";
      raw = raw.Trim();
      if(raw.Length == 0)
         throw new FormatException("StreamChunkThreshold is empty.");

      char suffix = char.ToUpperInvariant(raw[^1]);
      string numberText = char.IsLetter(suffix) ? raw[..^1] : raw;

      if(!long.TryParse(numberText, out long value) || value <= 0)
         throw new FormatException($"Invalid StreamChunkThreshold value: '{raw}'.");

      long multiplier = suffix switch
      {
         'K' => 1024L,
         'M' => 1024L * 1024L,
         'G' => 1024L * 1024L * 1024L,
         _ => 1024L * 1024L
      };

      return checked(value * multiplier);
   }

   // StreamWalk-based visitor retained in M1 history; lexer-based parser is now primary.
}
