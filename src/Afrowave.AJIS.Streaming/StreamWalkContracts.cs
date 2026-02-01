#nullable enable

namespace Afrowave.AJIS.Streaming;

/// <summary>
/// Kind of event produced by StreamWalk.
/// Matches canonical names used in Docs/tests/streamwalk.md.
/// </summary>
public enum StreamWalkEventKind
{
   BeginDocument,
   EndDocument,

   BeginObject,
   EndObject,

   BeginArray,
   EndArray,

   PropertyName,

   StringValue,
   NumberValue,
   TrueValue,
   FalseValue,
   NullValue,

   Comment,
   Directive,
}

/// <summary>
/// One streaming event produced by the walker.
/// </summary>
public readonly record struct StreamWalkEvent(
    StreamWalkEventKind Kind,
    ReadOnlyMemory<byte> Utf8Slice,
    long Offset
);

/// <summary>
/// Visitor interface for StreamWalk.
/// Implementations receive events in strict document order.
/// </summary>
public interface IStreamWalkVisitor
{
   /// <summary>
   /// Called for every event. Returning false requests early abort
   /// (only if AllowVisitorAbort is enabled).
   /// </summary>
   bool OnEvent(in StreamWalkEvent evt);

   /// <summary>
   /// Called exactly once on successful completion.
   /// </summary>
   void OnCompleted();

   /// <summary>
   /// Called exactly once on failure.
   /// </summary>
   void OnError(StreamWalkError error);
}

/// <summary>
/// Structured error reported by StreamWalk.
/// </summary>
public sealed record StreamWalkError(
    string Code,
    long Offset,
    int? Line,
    int? Column
);
