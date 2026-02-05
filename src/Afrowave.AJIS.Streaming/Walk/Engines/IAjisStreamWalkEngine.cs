#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

internal interface IAjisStreamWalkEngine
{
   string EngineId { get; }

   void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions);
}
