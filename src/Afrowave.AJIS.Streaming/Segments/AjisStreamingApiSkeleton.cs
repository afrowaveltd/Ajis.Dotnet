#nullable enable

using Afrowave.AJIS.Core;
using System.Runtime.CompilerServices;

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
      // Skeleton: keep API compiling and predictable.
      // Implementation will come later (StreamWalk/Reader algorithm).

      _ = input ?? throw new ArgumentNullException(nameof(input));
      _ = settings;
      _ = ct;

      // Ensure this is a valid async-iterator even before real parsing exists.
      await Task.CompletedTask;
      yield break;
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
