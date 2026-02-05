#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

public interface IAjisStreamWalkEngine
{
   string EngineId { get; }

   void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions);
}
