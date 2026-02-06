#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming.Reader;
using Afrowave.AJIS.Streaming.Walk;
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
   string? Text)
{
   public static AjisSegment Enter(AjisContainerKind kind, long pos, int depth)
       => new(AjisSegmentKind.EnterContainer, pos, depth, kind, null, null);

   public static AjisSegment Exit(AjisContainerKind kind, long pos, int depth)
       => new(AjisSegmentKind.ExitContainer, pos, depth, kind, null, null);

   public static AjisSegment Name(long pos, int depth, string name)
       => new(AjisSegmentKind.PropertyName, pos, depth, null, null, name);

   public static AjisSegment Value(long pos, int depth, AjisValueKind kind, string? text)
       => new(AjisSegmentKind.Value, pos, depth, null, kind, text);
}

/// <summary>
/// Streaming parser entry points.
/// </summary>
public static class AjisParse
{
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
      _ = settings?.ParserProfile ?? AjisProcessingProfile.Universal;
      _ = ct;

      if(settings?.ParserProfile == AjisProcessingProfile.Universal)
      {
         foreach(var segment in AjisLexerParserStream.Parse(
            input,
            numberOptions: settings?.Numbers,
            stringOptions: settings?.Strings,
            commentOptions: settings?.Comments,
            textMode: settings?.TextMode ?? global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
            allowTrailingCommas: settings?.AllowTrailingCommas ?? false,
            allowDirectives: settings?.AllowDirectives ?? true))
            yield return segment;
         yield break;
      }

      if(input is MemoryStream mem && mem.TryGetBuffer(out ArraySegment<byte> buffer))
      {
         foreach(var segment in ParseSegments(buffer.AsSpan(), settings))
            yield return segment;
         yield break;
      }

      if(input is FileStream fileStream && fileStream.CanSeek)
      {
         foreach(var segment in ParseSegmentsMappedFile(fileStream.Name, settings))
            yield return segment;
         yield break;
      }

      string tempPath = Path.GetTempFileName();
      try
      {
         await using(var tempStream = File.Create(tempPath))
            await input.CopyToAsync(tempStream, ct).ConfigureAwait(false);

         foreach(var segment in ParseSegmentsMappedFile(tempPath, settings))
            yield return segment;
      }
      finally
      {
         if(File.Exists(tempPath))
            File.Delete(tempPath);
      }
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
      _ = settings?.ParserProfile ?? AjisProcessingProfile.Universal;
      return ParseSegmentsInternal(utf8, settings);
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
         settings?.AllowDirectives ?? true);
      return parser.Parse();
   }

   private static string GetNumberText(Utf8JsonReader reader)
   {
      if(!reader.HasValueSequence)
         return Encoding.UTF8.GetString(reader.ValueSpan);

      return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
   }

   private static IEnumerable<AjisSegment> ParseSegmentsMappedFile(string path, AjisSettings? settings)
   {
      long length = new FileInfo(path).Length;
      using var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

      if(length == 0)
         return Array.Empty<AjisSegment>();
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

            return ParseSegments(span, settings).ToList();
         }
         finally
         {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
         }
      }

      return Array.Empty<AjisSegment>();
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
         return Array.Empty<AjisSegment>();

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
                     segments.Add(AjisSegment.Name(tokenOffset, depth, reader.GetString() ?? string.Empty));
                     break;
                  case JsonTokenType.String:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.String, reader.GetString()));
                     break;
                  case JsonTokenType.Number:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Number, GetNumberText(reader)));
                     break;
                  case JsonTokenType.True:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Boolean, "true"));
                     break;
                  case JsonTokenType.False:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Boolean, "false"));
                     break;
                  case JsonTokenType.Null:
                     segments.Add(AjisSegment.Value(tokenOffset, depth, AjisValueKind.Null, null));
                     break;
               }
            }

            state = reader.CurrentState;
            int consumed = (int)reader.BytesConsumed;
            carry = span.Length - consumed;
            if(carry > 0)
               span.Slice(consumed).CopyTo(buffer);
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
