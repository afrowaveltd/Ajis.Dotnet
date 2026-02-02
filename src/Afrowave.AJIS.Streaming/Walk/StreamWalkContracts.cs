#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// Visitor interface for StreamWalk.
/// Implementations receive events in strict document order.
/// </summary>
public interface IAjisStreamWalkVisitor
{
   /// <summary>
   /// Called for every event.
   /// </summary>
   void OnEvent(AjisStreamWalkEvent evt);

   /// <summary>
   /// Called exactly once on successful completion.
   /// </summary>
   void OnCompleted();

   /// <summary>
   /// Called exactly once on failure.
   /// </summary>
   void OnError(AjisStreamWalkError error);
}

/// <summary>
/// One streaming event produced by the walker.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Kind"/> is a canonical textual name matching the test-case format.
/// </para>
/// <para>
/// <see cref="Slice"/> is a raw UTF-8 byte slice referencing the input.
/// </para>
/// </remarks>
public readonly record struct AjisStreamWalkEvent(
   string Kind,
   ReadOnlyMemory<byte> Slice,
   long Offset);

/// <summary>
/// Structured error reported by StreamWalk.
/// </summary>
public sealed record AjisStreamWalkError(
   string Code,
   long Offset,
   int? Line,
   int? Column);