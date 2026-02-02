#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// Stream-walking runner for AJIS / JSON input.
/// This file defines the public API shell and delegates the core implementation
/// to versioned partial implementations (M1, M2, ...).
/// </summary>
public static partial class AjisStreamWalkRunner
{
   /// <summary>
   /// Runs StreamWalk over an in-memory UTF-8 buffer.
   /// This is the canonical entry point used by tests and production code.
   /// </summary>
   public static void Run(
       ReadOnlySpan<byte> inputUtf8,
       AjisStreamWalkOptions options,
       IAjisStreamWalkVisitor visitor,
       AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      ArgumentNullException.ThrowIfNull(visitor);

      // Delegate to M1 implementation for now
      RunM1(inputUtf8, options, visitor, runnerOptions);
   }

   /// <summary>
   /// Convenience overload for ReadOnlyMemory.
   /// </summary>
   public static void Run(
       ReadOnlyMemory<byte> inputUtf8,
       AjisStreamWalkOptions options,
       IAjisStreamWalkVisitor visitor,
       AjisStreamWalkRunnerOptions runnerOptions = default)
       => Run(inputUtf8.Span, options, visitor, runnerOptions);
}