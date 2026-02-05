#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

/// <summary>
/// A tiny, stable cost model used for engine selection.
/// Lower values should generally mean a better fit.
/// </summary>
public readonly record struct AjisEngineCost(
   int EstimatedPasses,
   long EstimatedMemoryBytes,
   bool RequiresRandomAccess);
