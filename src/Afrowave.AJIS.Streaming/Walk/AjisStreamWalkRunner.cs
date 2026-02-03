#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// StreamWalk runner (public entry point).
/// </summary>
public static class AjisStreamWalkRunner
{
   /// <summary>
   /// Runs StreamWalk over an in-memory UTF-8 payload.
   /// </summary>
   public static void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      ArgumentNullException.ThrowIfNull(visitor);

      // M1 = JSON-core subset used by current .case tests.
      AjisStreamWalkRunnerM1.Run(inputUtf8, options, visitor, runnerOptions);
   }

   /// <summary>
   /// Convenience overload for callers holding memory.
   /// </summary>
   public static void Run(
      ReadOnlyMemory<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions = default)
      => Run(inputUtf8.Span, options, visitor, runnerOptions);
}