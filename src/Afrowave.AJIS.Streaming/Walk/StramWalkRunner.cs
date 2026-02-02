#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// StreamWalk runner (API skeleton).
/// </summary>
public static class AjisStreamWalkRunner
{
   /// <summary>
   /// Primary overload: in-memory UTF-8 span.
   /// </summary>
   public static void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      _ = visitor ?? throw new ArgumentNullException(nameof(visitor));
      _ = options;
      _ = runnerOptions;
      _ = inputUtf8;

      // Skeleton: no parsing yet.
      throw new NotImplementedException("StreamWalk runner not implemented yet. This is an API skeleton.");
   }

   /// <summary>
   /// Convenience overload for callers holding ReadOnlyMemory.
   /// Avoids Span/Memory mismatch in test harness.
   /// </summary>
   public static void Run(
      ReadOnlyMemory<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions = default)
      => Run(inputUtf8.Span, options, visitor, runnerOptions);
}