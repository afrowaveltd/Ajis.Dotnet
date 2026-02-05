#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

internal sealed class AjisStreamWalkEngineM1 : IAjisStreamWalkEngine
{
   public string EngineId => AjisStreamWalkEngineIds.M1_Span_Serial;

   public void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions)
      => AjisStreamWalkRunnerM1.Run(inputUtf8, options, visitor, runnerOptions);
}
