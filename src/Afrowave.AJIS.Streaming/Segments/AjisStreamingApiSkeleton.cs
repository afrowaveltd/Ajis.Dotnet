#nullable enable

using Afrowave.AJIS.Core;
using System.Runtime.CompilerServices;

namespace Afrowave.AJIS.Streaming;

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
   /// Begins an object or array.
   /// </summary>
   BeginContainer = 0,

   /// <summary>
   /// Ends an object or array.
   /// </summary>
   EndContainer = 1,

   /// <summary>
   /// Object property name.
   /// </summary>
   PropertyName = 2,

   /// <summary>
   /// A primitive value.
   /// </summary>
   Value = 3,
}

/// <summary>
/// A streaming output unit produced while parsing AJIS text.
/// </summary>
/// <remarks>
/// Segments allow callers to process massive inputs without materializing a full DOM.
/// </remarks>
public readonly record struct AjisSegment(
    AjisSegmentKind Kind,
    AjisTextPosition Position,
    int Depth,
    AjisContainerKind? ContainerKind,
    AjisValueKind? ValueKind,
    string? Text)
{
   /// <summary>
   /// Creates a begin-container segment.
   /// </summary>
   public static AjisSegment Begin(AjisTextPosition pos, int depth, AjisContainerKind kind)
       => new(AjisSegmentKind.BeginContainer, pos, depth, kind, null, null);

   /// <summary>
   /// Creates an end-container segment.
   /// </summary>
   public static AjisSegment End(AjisTextPosition pos, int depth, AjisContainerKind kind)
       => new(AjisSegmentKind.EndContainer, pos, depth, kind, null, null);

   /// <summary>
   /// Creates a property-name segment.
   /// </summary>
   public static AjisSegment Name(AjisTextPosition pos, int depth, string name)
       => new(AjisSegmentKind.PropertyName, pos, depth, null, null, name);

   /// <summary>
   /// Creates a primitive value segment.
   /// </summary>
   /// <remarks>
   /// <paramref name="text"/> is the raw textual representation (unescaped for strings depends on settings).
   /// </remarks>
   public static AjisSegment Value(AjisTextPosition pos, int depth, AjisValueKind kind, string? text)
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
      _ = input;
      _ = settings;
      _ = ct;
      throw new NotImplementedException("Streaming parser not implemented yet. This is an API skeleton.");
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
      _ = utf8;
      _ = settings;
      throw new NotImplementedException("In-memory streaming parser not implemented yet. This is an API skeleton.");
   }
}

/// <summary>
/// Helpers for consuming segment streams.
/// </summary>
public static class AjisSegmentExtensions
{
   /// <summary>
   /// Consumes segments and calls <paramref name="onSegment"/> for each.
   /// </summary>
   /// <remarks>
   /// This is useful when the consumer wants a single callback-based pipeline.
   /// </remarks>
   public static async ValueTask ForEachAsync(
       this IAsyncEnumerable<AjisSegment> segments,
       Func<AjisSegment, ValueTask> onSegment,
       CancellationToken ct = default)
   {
      await foreach(var s in segments.WithCancellation(ct).ConfigureAwait(false))
      {
         await onSegment(s).ConfigureAwait(false);
      }
   }
}