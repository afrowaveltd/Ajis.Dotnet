#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

/// <summary>
/// A tiny, stable cost model used for engine selection.
/// Lower values should generally mean a better fit.
/// </summary>
public readonly record struct AjisEngineCost(
   int EstimatedPasses,
   long EstimatedMemoryBytes,
   bool RequiresRandomAccess)
{
   /// <summary>
   /// Combined score used for deterministic engine selection.
   /// Lower values indicate a better fit.
   /// </summary>
   public long Score
   {
      get
      {
         const long passWeight = 1_000_000_000L;
         const long randomAccessPenalty = 10_000_000_000L;

         long score = EstimatedPasses * passWeight + EstimatedMemoryBytes;
         if(RequiresRandomAccess)
            score += randomAccessPenalty;

         return score;
      }
   }
}
