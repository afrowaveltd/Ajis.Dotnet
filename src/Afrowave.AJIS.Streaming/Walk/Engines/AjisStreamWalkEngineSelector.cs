#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

internal static class AjisStreamWalkEngineSelector
{
   public static IAjisStreamWalkEngine Select(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      AjisStreamWalkRunnerOptions runnerOptions)
   {
      // Explicit preference wins.
      if(runnerOptions.EnginePreference == AjisStreamWalkEnginePreference.LowMemory)
         return AjisStreamWalkEngineRegistry.All.First(d => d.EngineId == AjisStreamWalkEngineIds.LowMem_Span_Serial).CreateEngine();

      if(runnerOptions.EnginePreference == AjisStreamWalkEnginePreference.Speed)
         return AjisStreamWalkEngineRegistry.All.First(d => d.EngineId == AjisStreamWalkEngineIds.M1_Span_Serial).CreateEngine();

      // Balanced: choose best supported cost.
      IAjisStreamWalkEngineDescriptor? best = null;
      AjisEngineCost bestCost = default;

      foreach(IAjisStreamWalkEngineDescriptor d in AjisStreamWalkEngineRegistry.All)
      {
         if(!d.Supports(options))
            continue;

         AjisEngineCost cost = d.EstimateCost(inputUtf8, options, runnerOptions);

         if(best is null || cost.Score < bestCost.Score)
         {
            best = d;
            bestCost = cost;
         }
      }

      // Should never happen (at least one engine supports M1 non-LAX).
      return (best ?? AjisStreamWalkEngineRegistry.All[0]).CreateEngine();
   }
}
