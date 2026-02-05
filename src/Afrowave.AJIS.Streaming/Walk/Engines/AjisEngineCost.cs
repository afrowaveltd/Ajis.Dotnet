#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

internal readonly record struct AjisEngineCost(
   int Score,
   string Reason);
