#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

public interface IAjisStreamWalkEngineDescriptor
{
   string EngineId { get; }

   AjisStreamWalkEngineKind Kind { get; }

   AjisEngineCapabilities Capabilities { get; }

   /// <summary>
   /// Returns true if this engine can run with the given options.
   /// </summary>
   Supports(AjisStreamWalkOptions options);

   /// <summary>
   /// Returns an estimated cost score for this input/options.
   /// Lower is better.
   /// </summary>
   AjisEngineCost EstimateCost(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      AjisStreamWalkRunnerOptions runnerOptions);

   /// <summary>
   /// Creates (or returns) the engine implementation.
   /// </summary>
   IAjisStreamWalkEngine CreateEngine();
}
